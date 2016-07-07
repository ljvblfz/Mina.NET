using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Common.Logging;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Ssl
{
    /// <summary>
    /// An SSL filter that encrypts and decrypts the data exchanged in the session.
    /// Adding this filter triggers SSL handshake procedure immediately.
    /// </summary>
    public class SslFilter : IOFilterAdapter
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(SslFilter));

        private static readonly AttributeKey NextFilter = new AttributeKey(typeof(SslFilter), "nextFilter");
        private static readonly AttributeKey SslHandler = new AttributeKey(typeof(SslFilter), "handler");

        /// <summary>
        /// Creates a new SSL filter using the specified PKCS7 signed file.
        /// </summary>
        /// <param name="certFile">the path of the PKCS7 signed file from which to create the X.509 certificate</param>
        public SslFilter(string certFile)
            : this(X509Certificate.CreateFromCertFile(certFile))
        {
        }

        /// <summary>
        /// Creates a new SSL filter using the specified certificate.
        /// </summary>
        /// <param name="cert">the <see cref="X509Certificate"/> to use</param>
        public SslFilter(X509Certificate cert)
        {
            Certificate = cert;
            CheckCertificateRevocation = true;
        }

        /// <summary>
        /// Creates a new SSL filter to a server.
        /// </summary>
        /// <param name="targetHost">the name of the server that shares this SSL connection</param>
        /// <param name="clientCertificates">the <see cref="X509CertificateCollection"/> containing client certificates</param>
        public SslFilter(string targetHost, X509CertificateCollection clientCertificates)
        {
            TargetHost = targetHost;
            ClientCertificates = clientCertificates;
            UseClientMode = true;
            CheckCertificateRevocation = false;
        }

        /// <summary>
        /// Gets or sets the protocol used for authentication.
        /// </summary>
        public SslProtocols SslProtocol { get; set; } = SslProtocols.Default;

        /// <summary>
        /// Gets the X.509 certificate.
        /// </summary>
        public X509Certificate Certificate { get; }

        /// <summary>
        /// Gets or sets a <see cref="bool"/> value that specifies
        /// whether the client must supply a certificate.
        /// The default value is <code>false</code>.
        /// </summary>
        public bool ClientCertificateRequired { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="bool"/> value that specifies
        /// whether the certificate revocation list is checked during authentication.
        /// The default value is <code>true</code> in server mode,
        /// <code>false</code> in client mode.
        /// </summary>
        public bool CheckCertificateRevocation { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="bool"/> value that specifies
        /// whether to use client (or server) mode when handshaking.
        /// The default value is <code>false</code> (server mode).
        /// </summary>
        public bool UseClientMode { get; set; }

        /// <summary>
        /// Gets or sets the name of the server that shares this SSL connection.
        /// </summary>
        public string TargetHost { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="X509CertificateCollection"/> containing client certificates
        /// </summary>
        public X509CertificateCollection ClientCertificates { get; set; }

        /// <summary>
        /// Returns <code>true</code> if and only if the specified session is
        /// encrypted/decrypted over SSL/TLS currently.
        /// </summary>
        public bool IsSslStarted(IOSession session)
        {
            var handler = session.GetAttribute<SslHandler>(SslHandler);
            return handler != null && handler.Authenticated;
        }

        /// <inheritdoc/>
        public override void OnPreAdd(IOFilterChain parent, string name, INextFilter nextFilter)
        {
            if (parent.Contains<SslFilter>())
            {
                throw new InvalidOperationException("Only one SSL filter is permitted in a chain.");
            }

            var session = parent.Session;
            session.SetAttribute(NextFilter, nextFilter);
            // Create a SSL handler and start handshake.
            var handler = new SslHandler(this, session);
            session.SetAttribute(SslHandler, handler);
        }

        /// <inheritdoc/>
        public override void OnPostAdd(IOFilterChain parent, string name, INextFilter nextFilter)
        {
            var handler = GetSslSessionHandler(parent.Session);
            handler.Handshake(nextFilter);
        }

        /// <inheritdoc/>
        public override void OnPreRemove(IOFilterChain parent, string name, INextFilter nextFilter)
        {
            var session = parent.Session;
            session.RemoveAttribute(NextFilter);
            session.RemoveAttribute(SslHandler);
        }

        /// <inheritdoc/>
        public override void SessionClosed(INextFilter nextFilter, IOSession session)
        {
            var handler = GetSslSessionHandler(session);
            try
            {
                // release resources
                handler.Destroy();
            }
            finally
            {
                // notify closed session
                base.SessionClosed(nextFilter, session);
            }
        }

        /// <inheritdoc/>
        public override void MessageReceived(INextFilter nextFilter, IOSession session, object message)
        {
            var buf = (IOBuffer) message;
            var handler = GetSslSessionHandler(session);
            // forward read encrypted data to SSL handler
            handler.MessageReceived(nextFilter, buf);
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            var encryptedWriteRequest = writeRequest as EncryptedWriteRequest;
            if (encryptedWriteRequest == null)
            {
                // ignore extra buffers used for handshaking
            }
            else
            {
                base.MessageSent(nextFilter, session, encryptedWriteRequest.InnerRequest);
            }
        }

        /// <inheritdoc/>
        public override void ExceptionCaught(INextFilter nextFilter, IOSession session, Exception cause)
        {
            base.ExceptionCaught(nextFilter, session, cause);
        }

        /// <inheritdoc/>
        public override void FilterWrite(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            var handler = GetSslSessionHandler(session);
            handler.ScheduleFilterWrite(nextFilter, writeRequest);
        }

        /// <inheritdoc/>
        public override void FilterClose(INextFilter nextFilter, IOSession session)
        {
            var handler = session.GetAttribute<SslHandler>(SslHandler);
            if (handler == null)
            {
                // The connection might already have closed, or
                // SSL might have not started yet.
                base.FilterClose(nextFilter, session);
                return;
            }

            IWriteFuture future = null;
            try
            {
                future = InitiateClosure(handler, nextFilter, session);
                future.Complete += (s, e) => base.FilterClose(nextFilter, session);
            }
            finally
            {
                if (future == null)
                {
                    base.FilterClose(nextFilter, session);
                }
            }
        }

        private IWriteFuture InitiateClosure(SslHandler handler, INextFilter nextFilter, IOSession session)
        {
            var future = DefaultWriteFuture.NewWrittenFuture(session);
            handler.Destroy();
            return future;
        }

        private SslHandler GetSslSessionHandler(IOSession session)
        {
            var handler = session.GetAttribute<SslHandler>(SslHandler);

            if (handler == null)
            {
                throw new InvalidOperationException();
            }

            if (handler.SslFilter != this)
            {
                throw new ArgumentException("Not managed by this filter.");
            }

            return handler;
        }

        public static void DisplaySecurityLevel(SslStream stream)
        {
            Log.DebugFormat("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength);
            Log.DebugFormat("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength);
            Log.DebugFormat("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength);
            Log.DebugFormat("Protocol: {0}", stream.SslProtocol);
        }

        public static void DisplaySecurityServices(SslStream stream)
        {
            Log.DebugFormat("Is authenticated: {0} as server? {1}", stream.IsAuthenticated, stream.IsServer);
            Log.DebugFormat("IsSigned: {0}", stream.IsSigned);
            Log.DebugFormat("Is Encrypted: {0}", stream.IsEncrypted);
        }

        public static void DisplayStreamProperties(SslStream stream)
        {
            Log.DebugFormat("Can read: {0}, write {1}", stream.CanRead, stream.CanWrite);
            Log.DebugFormat("Can timeout: {0}", stream.CanTimeout);
        }

        public static void DisplayCertificateInformation(SslStream stream)
        {
            Log.DebugFormat("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus);

            var localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                Log.DebugFormat("Local cert was issued to {0} and is valid from {1} until {2}.",
                    localCertificate.Subject,
                    localCertificate.GetEffectiveDateString(),
                    localCertificate.GetExpirationDateString());
            }
            else
            {
                Log.DebugFormat("Local certificate is null.");
            }
            // Display the properties of the client's certificate.
            var remoteCertificate = stream.RemoteCertificate;
            if (stream.RemoteCertificate != null)
            {
                Log.DebugFormat("Remote cert was issued to {0} and is valid from {1} until {2}.",
                    remoteCertificate.Subject,
                    remoteCertificate.GetEffectiveDateString(),
                    remoteCertificate.GetExpirationDateString());
            }
            else
            {
                Log.DebugFormat("Remote certificate is null.");
            }
        }

        internal class EncryptedWriteRequest : WriteRequestWrapper
        {
            private readonly IOBuffer _encryptedMessage;

            public EncryptedWriteRequest(IWriteRequest writeRequest, IOBuffer encryptedMessage)
                : base(writeRequest)
            {
                _encryptedMessage = encryptedMessage;
            }

            public override object Message => _encryptedMessage;
        }
    }

    class SslHandler : IDisposable
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(SslFilter));

        private readonly IOSession _session;
        private readonly IOSessionStream _sessionStream;
        private readonly SslStream _sslStream;
        private volatile bool _authenticated;
        private readonly ConcurrentQueue<IOFilterEvent> _preHandshakeEventQueue = new ConcurrentQueue<IOFilterEvent>();
        private INextFilter _currentNextFilter;
        private IWriteRequest _currentWriteRequest;

        public SslHandler(SslFilter sslFilter, IOSession session)
        {
            SslFilter = sslFilter;
            _session = session;
            _sessionStream = new IOSessionStream(this);
            _sslStream = new SslStream(_sessionStream, false);
        }

        public SslFilter SslFilter { get; }

        public bool Authenticated
        {
            get { return _authenticated; }
            private set
            {
                _authenticated = value;
                if (value)
                {
                    FlushPreHandshakeEvents();
                }
            }
        }

        public void Dispose()
        {
            _sessionStream.Dispose();
            _sslStream.Dispose();
        }

        public void Handshake(INextFilter nextFilter)
        {
            lock (this)
            {
                _currentNextFilter = nextFilter;

                if (SslFilter.UseClientMode)
                {
                    _sslStream.BeginAuthenticateAsClient(SslFilter.TargetHost,
                        SslFilter.ClientCertificates, SslFilter.SslProtocol,
                        SslFilter.CheckCertificateRevocation, AuthenticateAsClientCallback, null);
                }
                else
                {
                    _sslStream.BeginAuthenticateAsServer(SslFilter.Certificate,
                        SslFilter.ClientCertificateRequired, SslFilter.SslProtocol,
                        SslFilter.CheckCertificateRevocation, AuthenticateCallback, null);
                }
            }
        }

        private void AuthenticateAsClientCallback(IAsyncResult ar)
        {
            try
            {
                _sslStream.EndAuthenticateAsClient(ar);
            }
            catch (AuthenticationException e)
            {
                SslFilter.ExceptionCaught(_currentNextFilter, _session, e);
                return;
            }
            catch (Exception e)
            {
                SslFilter.ExceptionCaught(_currentNextFilter, _session, e);
                return;
            }

            Authenticated = true;

            if (Log.IsDebugEnabled)
            {
                // Display the properties and settings for the authenticated stream.
                SslFilter.DisplaySecurityLevel(_sslStream);
                SslFilter.DisplaySecurityServices(_sslStream);
                SslFilter.DisplayCertificateInformation(_sslStream);
                SslFilter.DisplayStreamProperties(_sslStream);
            }
        }

        private void AuthenticateCallback(IAsyncResult ar)
        {
            try
            {
                _sslStream.EndAuthenticateAsServer(ar);
            }
            catch (AuthenticationException e)
            {
                SslFilter.ExceptionCaught(_currentNextFilter, _session, e);
                return;
            }
            catch (IOException e)
            {
                SslFilter.ExceptionCaught(_currentNextFilter, _session, e);
                return;
            }

            Authenticated = true;

            if (Log.IsDebugEnabled)
            {
                // Display the properties and settings for the authenticated stream.
                SslFilter.DisplaySecurityLevel(_sslStream);
                SslFilter.DisplaySecurityServices(_sslStream);
                SslFilter.DisplayCertificateInformation(_sslStream);
                SslFilter.DisplayStreamProperties(_sslStream);
            }
        }

        public void ScheduleFilterWrite(INextFilter nextFilter, IWriteRequest writeRequest)
        {
            if (!_authenticated)
            {
                if (_session.Connected)
                {
                    // Handshake not complete yet.
                    _preHandshakeEventQueue.Enqueue(new IOFilterEvent(nextFilter, IOEventType.Write, _session,
                        writeRequest));
                }
                return;
            }

            var buf = (IOBuffer) writeRequest.Message;
            if (!buf.HasRemaining)
            {
                // empty message will break this SSL stream?
                return;
            }
            lock (this)
            {
                var array = buf.GetRemaining();
                _currentNextFilter = nextFilter;
                _currentWriteRequest = writeRequest;
                // SSL encrypt
                _sslStream.Write(array.Array, array.Offset, array.Count);
            }
        }

        public void MessageReceived(INextFilter nextFilter, IOBuffer buf)
        {
            lock (this)
            {
                _currentNextFilter = nextFilter;
                _sessionStream.Write(buf);
                if (_authenticated)
                {
                    var readBuffer = ReadBuffer();
                    nextFilter.MessageReceived(_session, readBuffer);
                }
            }
        }

        public void Destroy()
        {
            _sslStream.Close();
            IOFilterEvent scheduledWrite;
            while (_preHandshakeEventQueue.TryDequeue(out scheduledWrite))
            {
            }
        }

        private void FlushPreHandshakeEvents()
        {
            lock (this)
            {
                IOFilterEvent scheduledWrite;
                while (_preHandshakeEventQueue.TryDequeue(out scheduledWrite))
                {
                    SslFilter.FilterWrite(scheduledWrite.NextFilter, scheduledWrite.Session,
                        (IWriteRequest) scheduledWrite.Parameter);
                }
            }
        }

        private void WriteBuffer(IOBuffer buf)
        {
            IWriteRequest writeRequest;
            if (_authenticated)
            {
                writeRequest = new SslFilter.EncryptedWriteRequest(_currentWriteRequest, buf);
            }
            else
            {
                writeRequest = new DefaultWriteRequest(buf);
            }
            _currentNextFilter.FilterWrite(_session, writeRequest);
        }

        private IOBuffer ReadBuffer()
        {
            var buf = IOBuffer.Allocate(_sessionStream.Remaining);

            while (true)
            {
                var array = buf.GetRemaining();
                var bytesRead = _sslStream.Read(array.Array, array.Offset, array.Count);
                if (bytesRead <= 0)
                {
                    break;
                }
                buf.Position += bytesRead;

                if (_sessionStream.Remaining == 0)
                {
                    break;
                }
                // We have to grow the target buffer, it's too small.
                buf.Capacity <<= 1;
                buf.Limit = buf.Capacity;
            }

            buf.Flip();
            return buf;
        }

        class IOSessionStream : System.IO.Stream
        {
            readonly object _syncRoot = new byte[0];
            readonly SslHandler _sslHandler;
            readonly IOBuffer _buf;
            volatile bool _closed;
            volatile bool _released;
            IOException _exception;

            public IOSessionStream(SslHandler sslHandler)
            {
                _sslHandler = sslHandler;
                _buf = IOBuffer.Allocate(16);
                _buf.AutoExpand = true;
                _buf.Limit = 0;
            }

            public override int ReadByte()
            {
                lock (_syncRoot)
                {
                    if (!WaitForData())
                    {
                        return 0;
                    }
                    return _buf.Get() & 0xff;
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                lock (_syncRoot)
                {
                    if (!WaitForData())
                    {
                        return 0;
                    }

                    var readBytes = Math.Min(count, _buf.Remaining);
                    _buf.Get(buffer, offset, readBytes);
                    return readBytes;
                }
            }

            public override void Close()
            {
                base.Close();

                if (_closed)
                {
                    return;
                }

                lock (_syncRoot)
                {
                    _closed = true;
                    ReleaseBuffer();
                    Monitor.PulseAll(_syncRoot);
                }
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _sslHandler.WriteBuffer(IOBuffer.Wrap((byte[]) buffer.Clone(), offset, count));
            }

            public override void WriteByte(byte value)
            {
                var buf = IOBuffer.Allocate(1);
                buf.Put(value);
                buf.Flip();
                _sslHandler.WriteBuffer(buf);
            }

            public override void Flush()
            {
            }

            public void Write(IOBuffer buf)
            {
                if (_closed)
                {
                    return;
                }

                lock (_syncRoot)
                {
                    if (_buf.HasRemaining)
                    {
                        _buf.Compact().Put(buf).Flip();
                    }
                    else
                    {
                        _buf.Clear().Put(buf).Flip();
                        Monitor.PulseAll(_syncRoot);
                    }
                }
            }

            private bool WaitForData()
            {
                if (_released)
                {
                    return false;
                }

                lock (_syncRoot)
                {
                    while (!_released && _buf.Remaining == 0 && _exception == null)
                    {
                        try
                        {
                            Monitor.Wait(_syncRoot);
                        }
                        catch (ThreadInterruptedException e)
                        {
                            throw new IOException("Interrupted while waiting for more data", e);
                        }
                    }
                }

                if (_exception != null)
                {
                    ReleaseBuffer();
                    throw _exception;
                }

                if (_closed && _buf.Remaining == 0)
                {
                    ReleaseBuffer();
                    return false;
                }

                return true;
            }

            private void ReleaseBuffer()
            {
                if (_released)
                {
                    return;
                }
                _released = true;
            }

            public IOException Exception
            {
                set
                {
                    if (_exception == null)
                    {
                        lock (_syncRoot)
                        {
                            _exception = value;
                            Monitor.PulseAll(_syncRoot);
                        }
                    }
                }
            }

            public int Remaining => _buf.Remaining;

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length
            {
                get { throw new NotSupportedException(); }
            }

            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }
        }
    }
}
