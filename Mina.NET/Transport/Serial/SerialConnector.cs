#if !UNITY
using System;
using System.IO;
using System.IO.Ports;
using System.Net;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Transport.Serial
{
    /// <summary>
    /// <see cref="IOConnector"/> for serial communication transport.
    /// </summary>
    public class SerialConnector : AbstractIOConnector, IIOProcessor<SerialSession>
    {
        private readonly IdleStatusChecker _idleStatusChecker;

        /// <summary>
        /// Instantiates.
        /// </summary>
        public SerialConnector()
            : base(new DefaultSerialSessionConfig())
        {
            _idleStatusChecker = new IdleStatusChecker(() => ManagedSessions.Values);
        }

        /// <inheritdoc/>
        public new ISerialSessionConfig SessionConfig => (ISerialSessionConfig)base.SessionConfig;

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata => SerialSession.Metadata;

        /// <inheritdoc/>
        protected override IConnectFuture Connect0(EndPoint remoteEp, EndPoint localEp, Action<IOSession, IConnectFuture> sessionInitializer)
        {
            var config = SessionConfig;
            var sep = (SerialEndPoint)remoteEp;

            var serialPort = new SerialPort(sep.PortName, sep.BaudRate, sep.Parity, sep.DataBits, sep.StopBits);
            if (config.ReadBufferSize > 0)
                serialPort.ReadBufferSize = config.ReadBufferSize;
            if (config.ReadTimeout > 0)
                serialPort.ReadTimeout = config.ReadTimeout * 1000;
            if (config.WriteBufferSize > 0)
                serialPort.WriteBufferSize = config.WriteBufferSize;
            if (config.WriteTimeout > 0)
                serialPort.WriteTimeout = config.WriteTimeout * 1000;
            if (config.ReceivedBytesThreshold > 0)
                serialPort.ReceivedBytesThreshold = config.ReceivedBytesThreshold;

            IConnectFuture future = new DefaultConnectFuture();
            var session = new SerialSession(this, sep, serialPort);
            InitSession(session, future, sessionInitializer);

            try
            {
                session.Processor.Add(session);
            }
            catch (IOException ex)
            {
                return DefaultConnectFuture.NewFailedFuture(ex);
            }

            _idleStatusChecker.Start();

            return future;
        }

        /// <summary>
        /// Disposes.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _idleStatusChecker.Dispose();
            }
            base.Dispose(disposing);
        }

        #region IoProcessor

        private void Add(SerialSession session)
        {
            // Build the filter chain of this session.
            session.Service.FilterChainBuilder.BuildFilterChain(session.FilterChain);

            // Propagate the SESSION_CREATED event up to the chain
            var serviceSupport = session.Service as IOServiceSupport;
            if (serviceSupport != null)
                serviceSupport.FireSessionCreated(session);

            session.Start();
        }

        private void Write(SerialSession session, IWriteRequest writeRequest)
        {
            var writeRequestQueue = session.WriteRequestQueue;
            writeRequestQueue.Offer(session, writeRequest);
            if (!session.WriteSuspended)
                Flush(session);
        }

        private void Flush(SerialSession session)
        {
            session.Flush();
        }

        private void Remove(SerialSession session)
        {
            session.SerialPort.Close();
            var support = session.Service as IOServiceSupport;
            if (support != null)
                support.FireSessionDestroyed(session);
        }

        private void UpdateTrafficControl(SerialSession session)
        {
            if (!session.WriteSuspended)
                Flush(session);
        }

        void IIOProcessor<SerialSession>.Add(SerialSession session)
        {
            Add(session);
        }

        void IIOProcessor<SerialSession>.Write(SerialSession session, IWriteRequest writeRequest)
        {
            Write(session, writeRequest);
        }

        void IIOProcessor<SerialSession>.Flush(SerialSession session)
        {
            Flush(session);
        }

        void IIOProcessor<SerialSession>.Remove(SerialSession session)
        {
            Remove(session);
        }

        void IIOProcessor<SerialSession>.UpdateTrafficControl(SerialSession session)
        {
            UpdateTrafficControl(session);
        }

        void IOProcessor.Add(IOSession session)
        {
            Add((SerialSession)session);
        }

        void IOProcessor.Write(IOSession session, IWriteRequest writeRequest)
        {
            Write((SerialSession)session, writeRequest);
        }

        void IOProcessor.Flush(IOSession session)
        {
            Flush((SerialSession)session);
        }

        void IOProcessor.Remove(IOSession session)
        {
            Remove((SerialSession)session);
        }

        void IOProcessor.UpdateTrafficControl(IOSession session)
        {
            UpdateTrafficControl((SerialSession)session);
        }

        #endregion
    }
}
#endif