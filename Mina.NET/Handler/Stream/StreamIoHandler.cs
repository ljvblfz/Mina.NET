﻿using System;
using System.IO;
using Common.Logging;
using Mina.Core.Buffer;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Handler.Stream
{
    /// <summary>
    /// A <see cref="IOHandler"/> that adapts asynchronous MINA events to stream I/O.
    /// </summary>
    public abstract class StreamIoHandler : IOHandlerAdapter
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StreamIoHandler));
        private static readonly AttributeKey KeyIn = new AttributeKey(typeof(StreamIoHandler), "in");
        private static readonly AttributeKey KeyOut = new AttributeKey(typeof(StreamIoHandler), "out");

        /// <summary>
        /// Gets or sets read timeout in seconds.
        /// </summary>
        public int ReadTimeout { get; set; }

        /// <summary>
        /// Gets or sets write timeout in seconds.
        /// </summary>
        public int WriteTimeout { get; set; }

        /// <inheritdoc/>
        public override void SessionOpened(IOSession session)
        {
            // Set timeouts
            session.Config.WriteTimeout = WriteTimeout;
            session.Config.SetIdleTime(IdleStatus.ReaderIdle, ReadTimeout);

            // Create streams
            var input = new IoSessionStream();
            var output = new IoSessionStream(session);
            session.SetAttribute(KeyIn, input);
            session.SetAttribute(KeyOut, output);
            ProcessStreamIo(session, input, output);
        }

        /// <inheritdoc/>
        public override void SessionClosed(IOSession session)
        {
            var input = session.GetAttribute<IoSessionStream>(KeyIn);
            var output = session.GetAttribute<IoSessionStream>(KeyOut);
            try
            {
                input.Close();
            }
            finally
            {
                output.Close();
            }
        }

        /// <inheritdoc/>
        public override void MessageReceived(IOSession session, object message)
        {
            var input = session.GetAttribute<IoSessionStream>(KeyIn);
            input.Write((IOBuffer)message);
        }

        /// <inheritdoc/>
        public override void ExceptionCaught(IOSession session, Exception cause)
        {
            var ioe = cause as IOException;
            if (ioe != null)
            {
                var input = session.GetAttribute<IoSessionStream>(KeyIn);
                if (input != null)
                {
                    input.Exception = ioe;
                    return;
                }
            }

            if (Log.IsWarnEnabled)
                Log.Warn("Unexpected exception.", cause);
            session.Close(true);
        }

        /// <inheritdoc/>
        public override void SessionIdle(IOSession session, IdleStatus status)
        {
            if (status == IdleStatus.ReaderIdle)
                throw new IOException("Read timeout");
        }

        /// <summary>
        /// Implement this method to execute your stream I/O logic.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="input"></param>
        /// <param name="output"></param>
        protected abstract void ProcessStreamIo(IOSession session, System.IO.Stream input, System.IO.Stream output);
    }
}
