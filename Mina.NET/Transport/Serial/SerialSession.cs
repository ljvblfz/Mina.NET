#if !UNITY
using System;
using System.IO.Ports;
using System.Net;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Transport.Serial
{
    class SerialSession : AbstractIOSession, ISerialSession
    {
        public static readonly ITransportMetadata Metadata
            = new DefaultTransportMetadata("mina", "serial", false, true, typeof(SerialEndPoint));

        private readonly SerialEndPoint _endpoint;
        private int _writing;

        public SerialSession(SerialConnector service, SerialEndPoint endpoint, SerialPort serialPort)
            : base(service)
        {
            Processor = service;
            base.Config = new SessionConfigImpl(serialPort);
            if (service.SessionConfig != null)
                Config.SetAll(service.SessionConfig);
            FilterChain = new DefaultIOFilterChain(this);
            SerialPort = serialPort;
            _endpoint = endpoint;

            SerialPort.DataReceived += _serialPort_DataReceived;
        }

        void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (ReadSuspended || e.EventType == SerialData.Eof)
                return;

            var bytesToRead = SerialPort.BytesToRead;
            var data = new byte[bytesToRead];
            var read = SerialPort.Read(data, 0, bytesToRead);
            if (read > 0)
            {
                try
                {
                    FilterChain.FireMessageReceived(IOBuffer.Wrap(data, 0, read));
                }
                catch (Exception ex)
                {
                    FilterChain.FireExceptionCaught(ex);
                }
            }
        }

        public override IOProcessor Processor { get; }

        public override IOFilterChain FilterChain { get; }

        public override EndPoint LocalEndPoint => null;

        public override EndPoint RemoteEndPoint => _endpoint;

        public override ITransportMetadata TransportMetadata => Metadata;

        public new ISerialSessionConfig Config => (ISerialSessionConfig)base.Config;

        public SerialPort SerialPort { get; }

        public bool RtsEnable
        {
            get { return SerialPort.RtsEnable; }
            set { SerialPort.RtsEnable = value; }
        }

        public bool DtrEnable
        {
            get { return SerialPort.DtrEnable; }
            set { SerialPort.DtrEnable = value; }
        }

        public void Start()
        {
            SerialPort.Open();
        }

        public void Flush()
        {
            if (WriteSuspended)
                return;
            if (Interlocked.CompareExchange(ref _writing, 1, 0) > 0)
                return;
            BeginSend();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            SerialPort.Dispose();
        }

        private void BeginSend()
        {
            var req = CurrentWriteRequest;
            if (req == null)
            {
                req = WriteRequestQueue.Poll(this);

                if (req == null)
                {
                    Interlocked.Exchange(ref _writing, 0);
                    return;
                }
            }

            var buf = req.Message as IOBuffer;

            if (buf == null)
            {
                throw new InvalidOperationException("Don't know how to handle message of type '"
                            + req.Message.GetType().Name + "'.  Are you missing a protocol encoder?");
            }
            CurrentWriteRequest = req;
            if (buf.HasRemaining)
                BeginSend(buf);
            else
                EndSend(0);
        }

        private void BeginSend(IOBuffer buf)
        {
            var array = buf.GetRemaining();
            try
            {
                SerialPort.BaseStream.BeginWrite(array.Array, array.Offset, array.Count, SendCallback, buf);
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                FilterChain.FireExceptionCaught(ex);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            var buf = (IOBuffer)ar.AsyncState;
            try
            {
                SerialPort.BaseStream.EndWrite(ar);
            }
            catch (Exception ex)
            {
                FilterChain.FireExceptionCaught(ex);

                // closed
                Processor.Remove(this);

                return;
            }

            var written = buf.Remaining;
            buf.Position += written;
            EndSend(written);
        }

        private void EndSend(int bytesTransferred)
        {
            IncreaseWrittenBytes(bytesTransferred, DateTime.Now);

            var req = CurrentWriteRequest;
            if (req != null)
            {
                var buf = req.Message as IOBuffer;
                if (!buf.HasRemaining)
                {
                    // Buffer has been sent, clear the current request.
                    var pos = buf.Position;
                    buf.Reset();

                    CurrentWriteRequest = null;

                    try
                    {
                        FilterChain.FireMessageSent(req);
                    }
                    catch (Exception ex)
                    {
                        FilterChain.FireExceptionCaught(ex);
                    }

                    // And set it back to its position
                    buf.Position = pos;
                }
            }

            if (SerialPort.IsOpen)
                BeginSend();
        }

        class SessionConfigImpl : IOSessionConfig, ISerialSessionConfig
        {
            private readonly SerialPort _serialPort;
            private int _idleTimeForRead;
            private int _idleTimeForWrite;
            private int _idleTimeForBoth;

            public SessionConfigImpl(SerialPort serialPort)
            {
                _serialPort = serialPort;
            }

            public void SetAll(IOSessionConfig config)
            {
                if (config == null)
                    throw new ArgumentNullException(nameof(config));
                SetIdleTime(IdleStatus.BothIdle, config.GetIdleTime(IdleStatus.BothIdle));
                SetIdleTime(IdleStatus.ReaderIdle, config.GetIdleTime(IdleStatus.ReaderIdle));
                SetIdleTime(IdleStatus.WriterIdle, config.GetIdleTime(IdleStatus.WriterIdle));
                ThroughputCalculationInterval = config.ThroughputCalculationInterval;

                // other properties will be set in SerialConnector.Connect()
            }

            public int ReadTimeout
            {
                get { return _serialPort.ReadTimeout; }
                set { _serialPort.ReadTimeout = value; }
            }

            public int ReadBufferSize
            {
                get { return _serialPort.ReadBufferSize; }
                set { _serialPort.ReadBufferSize = value; }
            }

            public int WriteTimeout
            {
                get { return _serialPort.WriteTimeout; }
                set { _serialPort.WriteTimeout = value; }
            }

            public long WriteTimeoutInMillis => _serialPort.WriteTimeout * 1000L;

            public int WriteBufferSize
            {
                get { return _serialPort.WriteBufferSize; }
                set { _serialPort.WriteBufferSize = value; }
            }

            public int ReceivedBytesThreshold
            {
                get { return _serialPort.ReceivedBytesThreshold; }
                set { _serialPort.ReceivedBytesThreshold = value; }
            }

            public int ThroughputCalculationInterval { get; set; } = 3;

            public long ThroughputCalculationIntervalInMillis => ThroughputCalculationInterval * 1000L;

            public int ReaderIdleTime
            {
                get { return GetIdleTime(IdleStatus.ReaderIdle); }
                set { SetIdleTime(IdleStatus.ReaderIdle, value); }
            }

            public int WriterIdleTime
            {
                get { return GetIdleTime(IdleStatus.WriterIdle); }
                set { SetIdleTime(IdleStatus.WriterIdle, value); }
            }

            public int BothIdleTime
            {
                get { return GetIdleTime(IdleStatus.BothIdle); }
                set { SetIdleTime(IdleStatus.BothIdle, value); }
            }

            public int GetIdleTime(IdleStatus status)
            {
                switch (status)
                {
                    case IdleStatus.ReaderIdle:
                        return _idleTimeForRead;
                    case IdleStatus.WriterIdle:
                        return _idleTimeForWrite;
                    case IdleStatus.BothIdle:
                        return _idleTimeForBoth;
                    default:
                        throw new ArgumentException("Unknown status", nameof(status));
                }
            }

            public long GetIdleTimeInMillis(IdleStatus status)
            {
                return GetIdleTime(status) * 1000L;
            }

            public void SetIdleTime(IdleStatus status, int idleTime)
            {
                switch (status)
                {
                    case IdleStatus.ReaderIdle:
                        _idleTimeForRead = idleTime;
                        break;
                    case IdleStatus.WriterIdle:
                        _idleTimeForWrite = idleTime;
                        break;
                    case IdleStatus.BothIdle:
                        _idleTimeForBoth = idleTime;
                        break;
                    default:
                        throw new ArgumentException("Unknown status", nameof(status));
                }
            }
        }
    }
}
#endif