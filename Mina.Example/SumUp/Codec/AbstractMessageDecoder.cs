using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Example.SumUp.Message;
using Mina.Filter.Codec;
using Mina.Filter.Codec.Demux;

namespace Mina.Example.SumUp.Codec
{
    abstract class AbstractMessageDecoder : IMessageDecoder
    {
        private readonly int _type;
        private int _sequence;
        private bool _readHeader;

        public AbstractMessageDecoder(int type)
        {
            _type = type;
        }

        public MessageDecoderResult Decodable(IOSession session, IOBuffer input)
        {
            // Return NeedData if the whole header is not read yet.
            if (input.Remaining < Constants.HeaderLen)
                return MessageDecoderResult.NeedData;

            // Return OK if type and bodyLength matches.
            if (_type == input.GetInt16())
                return MessageDecoderResult.Ok;

            // Return NotOK if not matches.
            return MessageDecoderResult.NotOk;
        }

        public MessageDecoderResult Decode(IOSession session, IOBuffer input, IProtocolDecoderOutput output)
        {
            // Try to skip header if not read.
            if (!_readHeader)
            {
                input.GetInt16(); // Skip 'type'.
                _sequence = input.GetInt32(); // Get 'sequence'.
                _readHeader = true;
            }

            // Try to decode body
            var m = DecodeBody(session, input);
            // Return NEED_DATA if the body is not fully read.
            if (m == null)
            {
                return MessageDecoderResult.NeedData;
            }
            _readHeader = false; // reset readHeader for the next decode
            m.Sequence = _sequence;
            output.Write(m);

            return MessageDecoderResult.Ok;
        }

        public virtual void FinishDecode(IOSession session, IProtocolDecoderOutput output)
        { }

        protected abstract AbstractMessage DecodeBody(IOSession session, IOBuffer input);
    }
}
