using System;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Session;
using Mina.Util;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// A virtual <see cref="IOSession"/> that provides <see cref="IProtocolEncoderOutput"/>
    /// and <see cref="IProtocolDecoderOutput"/>.  It is useful for unit-testing
    /// codec and reusing codec for non-network-use (e.g. serialization).
    /// </summary>
    public class ProtocolCodecSession : DummySession
    {
        private readonly IWriteFuture _notWrittenFuture;
        private readonly AbstractProtocolEncoderOutput _encoderOutput;
        private readonly AbstractProtocolDecoderOutput _decoderOutput;

        public ProtocolCodecSession()
        { 
            _notWrittenFuture = DefaultWriteFuture.NewNotWrittenFuture(this, new NotImplementedException());
            _encoderOutput = new DummyProtocolEncoderOutput(_notWrittenFuture);
            _decoderOutput = new DummyProtocolDecoderOutput();
        }

        public IProtocolEncoderOutput EncoderOutput => _encoderOutput;

        public IQueue<object> EncoderOutputQueue => _encoderOutput.MessageQueue;

        public IProtocolDecoderOutput DecoderOutput => _decoderOutput;

        public IQueue<object> DecoderOutputQueue => _decoderOutput.MessageQueue;

        class DummyProtocolEncoderOutput : AbstractProtocolEncoderOutput
        {
            private IWriteFuture _future;

            public DummyProtocolEncoderOutput(IWriteFuture future)
            {
                _future = future;
            }

            public override IWriteFuture Flush()
            {
                return _future;
            }
        }

        class DummyProtocolDecoderOutput : AbstractProtocolDecoderOutput
        {
            public override void Flush(INextFilter nextFilter, IOSession session)
            {
                // Do nothing
            }
        }
    }
}
