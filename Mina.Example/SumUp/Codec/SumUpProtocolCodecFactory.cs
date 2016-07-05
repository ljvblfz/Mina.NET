﻿using Mina.Example.SumUp.Message;
using Mina.Filter.Codec.Demux;

namespace Mina.Example.SumUp.Codec
{
    class SumUpProtocolCodecFactory : DemuxingProtocolCodecFactory
    {
        public SumUpProtocolCodecFactory(bool server)
        {
            if (server)
            {
                AddMessageDecoder<AddMessageDecoder>();
                AddMessageEncoder<ResultMessage, ResultMessageEncoder<ResultMessage>>();
            }
            else
            {
                AddMessageEncoder<AddMessage, AddMessageEncoder<AddMessage>>();
                AddMessageDecoder<ResultMessageDecoder>();
            }
        }
    }
}
