using System;
using Common.Logging;
using Mina.Core.Buffer;
using Mina.Core.File;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// An <see cref="IOFilter"/> which translates binary or protocol specific data into
    /// message objects and vice versa using <see cref="IProtocolCodecFactory"/>,
    /// <see cref="IProtocolEncoder"/>, or <see cref="IProtocolDecoder"/>.
    /// </summary>
    public class ProtocolCodecFilter : IOFilterAdapter
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ProtocolCodecFilter));
        private static readonly IOBuffer EmptyBuffer = IOBuffer.Wrap(new byte[0]);
        private readonly AttributeKey _decoderOut = new AttributeKey(typeof(ProtocolCodecFilter), "decoderOut");
        private readonly AttributeKey _encoderOut = new AttributeKey(typeof(ProtocolCodecFilter), "encoderOut");
        private readonly IProtocolCodecFactory _factory;

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="factory">the factory for creating <see cref="IProtocolEncoder"/> and <see cref="IProtocolDecoder"/></param>
        public ProtocolCodecFilter(IProtocolCodecFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _factory = factory;
        }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="encoder">the <see cref="IProtocolEncoder"/> for encoding message objects into binary or protocol specific data</param>
        /// <param name="decoder">the <see cref="IProtocolDecoder"/> for decoding binary or protocol specific data into message objects</param>
        public ProtocolCodecFilter(IProtocolEncoder encoder, IProtocolDecoder decoder)
            : this(new ProtocolCodecFactory(encoder, decoder))
        { }

        /// <inheritdoc/>
        public override void OnPreAdd(IOFilterChain parent, string name, INextFilter nextFilter)
        {
            if (parent.Contains(this))
                throw new ArgumentException("You can't add the same filter instance more than once.  Create another instance and add it.");
        }

        /// <inheritdoc/>
        public override void OnPostRemove(IOFilterChain parent, string name, INextFilter nextFilter)
        {
            // Clean everything
            DisposeCodec(parent.Session);
        }

        /// <inheritdoc/>
        public override void MessageReceived(INextFilter nextFilter, IOSession session, object message)
        {
            //if (log.IsDebugEnabled)
            //    log.DebugFormat("Processing a MESSAGE_RECEIVED for session {0}", session.Id);

            var input = message as IOBuffer;
            if (input == null)
            {
                nextFilter.MessageReceived(session, message);
                return;
            }

            var decoder = _factory.GetDecoder(session);
            var decoderOutput = GetDecoderOut(session, nextFilter);

            // Loop until we don't have anymore byte in the buffer,
            // or until the decoder throws an unrecoverable exception or
            // can't decoder a message, because there are not enough
            // data in the buffer
            while (input.HasRemaining)
            {
                var oldPos = input.Position;
                try
                {
                    // TODO may not need lock on UDP
                    lock (session)
                    {
                        // Call the decoder with the read bytes
                        decoder.Decode(session, input, decoderOutput);
                    }

                    // Finish decoding if no exception was thrown.
                    decoderOutput.Flush(nextFilter, session);
                }
                catch (Exception ex)
                {
                    var pde = ex as ProtocolDecoderException;
                    if (pde == null)
                        pde = new ProtocolDecoderException(null, ex);
                    if (pde.Hexdump == null)
                    {
                        // Generate a message hex dump
                        var curPos = input.Position;
                        input.Position = oldPos;
                        pde.Hexdump = input.GetHexDump();
                        input.Position = curPos;
                    }

                    decoderOutput.Flush(nextFilter, session);
                    nextFilter.ExceptionCaught(session, pde);

                    // Retry only if the type of the caught exception is
                    // recoverable and the buffer position has changed.
                    // We check buffer position additionally to prevent an
                    // infinite loop.
                    if (!(ex is RecoverableProtocolDecoderException) || input.Position == oldPos)
                        break;
                }
            }
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            if (writeRequest is EncodedWriteRequest)
                return;

            var wrappedRequest = writeRequest as MessageWriteRequest;
            if (wrappedRequest != null)
            {
                nextFilter.MessageSent(session, wrappedRequest.InnerRequest);
            }
            else
            {
                nextFilter.MessageSent(session, writeRequest);
            }
        }

        /// <inheritdoc/>
        public override void FilterWrite(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            var message = writeRequest.Message;

            // Bypass the encoding if the message is contained in a IoBuffer,
            // as it has already been encoded before
            if (message is IOBuffer || message is IFileRegion)
            {
                nextFilter.FilterWrite(session, writeRequest);
                return;
            }

            // Get the encoder in the session
            var encoder = _factory.GetEncoder(session);
            var encoderOut = GetEncoderOut(session, nextFilter, writeRequest);

            if (encoder == null)
                throw new ProtocolEncoderException("The encoder is null for the session " + session);
            if (encoderOut == null)
                throw new ProtocolEncoderException("The encoderOut is null for the session " + session);

            try
            {
                encoder.Encode(session, message, encoderOut);
                var ape = encoderOut as AbstractProtocolEncoderOutput;
                if (ape != null)
                {
                    // Send it directly
                    var bufferQueue = ape.MessageQueue;
                    // Write all the encoded messages now
                    while (!bufferQueue.IsEmpty)
                    {
                        var encodedMessage = bufferQueue.Dequeue();

                        if (encodedMessage == null)
                            break;

                        // Flush only when the buffer has remaining.
                        var buf = encodedMessage as IOBuffer;
                        if (buf == null || buf.HasRemaining)
                        {
                            IWriteRequest encodedWriteRequest = new EncodedWriteRequest(encodedMessage, null, writeRequest.Destination);
                            nextFilter.FilterWrite(session, encodedWriteRequest);
                        }
                    }
                }

                // Call the next filter
                nextFilter.FilterWrite(session, new MessageWriteRequest(writeRequest));
            }
            catch (Exception ex)
            {
                var pee = ex as ProtocolEncoderException;
                if (pee == null)
                    pee = new ProtocolEncoderException(null, ex);
                throw pee;
            }
        }

        /// <inheritdoc/>
        public override void SessionClosed(INextFilter nextFilter, IOSession session)
        {
            // Call finishDecode() first when a connection is closed.
            var decoder = _factory.GetDecoder(session);
            var decoderOut = GetDecoderOut(session, nextFilter);

            try
            {
                decoder.FinishDecode(session, decoderOut);
            }
            catch (Exception ex)
            {
                var pde = ex as ProtocolDecoderException;
                if (pde == null)
                    pde = new ProtocolDecoderException(null, ex);
                throw pde;
            }
            finally
            {
                // Dispose everything
                DisposeCodec(session);
                decoderOut.Flush(nextFilter, session);
            }

            // Call the next filter
            nextFilter.SessionClosed(session);
        }

        private IProtocolDecoderOutput GetDecoderOut(IOSession session, INextFilter nextFilter)
        {
            var output = session.GetAttribute<IProtocolDecoderOutput>(_decoderOut);

            if (output == null)
            {
                // Create a new instance, and stores it into the session
                output = new ProtocolDecoderOutputImpl();
                session.SetAttribute(_decoderOut, output);
            }

            return output;
        }

        private IProtocolEncoderOutput GetEncoderOut(IOSession session, INextFilter nextFilter, IWriteRequest writeRequest)
        {
            var output = session.GetAttribute<IProtocolEncoderOutput>(_encoderOut);

            if (output == null)
            {
                // Create a new instance, and stores it into the session
                output = new ProtocolEncoderOutputImpl(session, nextFilter, writeRequest);
                session.SetAttribute(_encoderOut, output);
            }

            return output;
        }

        private void DisposeCodec(IOSession session)
        {
            // We just remove the two instances of encoder/decoder to release resources
            // from the session
            //DisposeEncoder(session);
            //DisposeDecoder(session);

            // We also remove the callback
            DisposeDecoderOut(session);
        }

        private void DisposeDecoderOut(IOSession session)
        {
            session.RemoveAttribute(_decoderOut);
        }

        class ProtocolDecoderOutputImpl : AbstractProtocolDecoderOutput
        {
            public override void Flush(INextFilter nextFilter, IOSession session)
            {
                var messageQueue = MessageQueue;

                while (!messageQueue.IsEmpty)
                {
                    nextFilter.MessageReceived(session, messageQueue.Dequeue());
                }
            }
        }

        class ProtocolEncoderOutputImpl : AbstractProtocolEncoderOutput
        {
            private readonly IOSession _session;
            private readonly INextFilter _nextFilter;
            private readonly System.Net.EndPoint _destination;

            public ProtocolEncoderOutputImpl(IOSession session, INextFilter nextFilter, IWriteRequest writeRequest)
            {
                _session = session;
                _nextFilter = nextFilter;
                _destination = writeRequest.Destination;
            }

            public override IWriteFuture Flush()
            {
                var bufferQueue = MessageQueue;
                IWriteFuture future = null;

                while (!bufferQueue.IsEmpty)
                {
                    var encodedMessage = bufferQueue.Dequeue();

                    if (encodedMessage == null)
                        break;

                    // Flush only when the buffer has remaining.
                    var buf = encodedMessage as IOBuffer;
                    if (buf == null || buf.HasRemaining)
                    {
                        future = new DefaultWriteFuture(_session);
                        _nextFilter.FilterWrite(_session, new EncodedWriteRequest(encodedMessage, future, _destination));
                    }
                }

                if (future == null)
                {
                    // Creates an empty writeRequest containing the destination
                    IWriteRequest writeRequest = new DefaultWriteRequest(DefaultWriteRequest.EmptyMessage, null, _destination);
                    future = DefaultWriteFuture.NewNotWrittenFuture(_session, new NothingWrittenException(writeRequest));
                }

                return future;
            }
        }

        class EncodedWriteRequest : DefaultWriteRequest
        {
            public EncodedWriteRequest(object encodedMessage, IWriteFuture future, System.Net.EndPoint destination)
                : base(encodedMessage, future, destination)
            { }

            public override bool Encoded => true;
        }

        class MessageWriteRequest : WriteRequestWrapper
        {
            public MessageWriteRequest(IWriteRequest writeRequest)
                : base(writeRequest)
            { }

            public override object Message => EmptyBuffer;
        }

        class ProtocolCodecFactory : IProtocolCodecFactory
        {
            private IProtocolEncoder _encoder;
            private IProtocolDecoder _decoder;

            public ProtocolCodecFactory(IProtocolEncoder encoder, IProtocolDecoder decoder)
            {
                if (encoder == null)
                    throw new ArgumentNullException(nameof(encoder));
                if (decoder == null)
                    throw new ArgumentNullException(nameof(decoder));
                _encoder = encoder;
                _decoder = decoder;
            }

            public IProtocolEncoder GetEncoder(IOSession session)
            {
                return _encoder;
            }

            public IProtocolDecoder GetDecoder(IOSession session)
            {
                return _decoder;
            }
        }
    }
}
