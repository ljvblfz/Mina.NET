using System;

namespace Mina.Example.SumUp.Message
{
    [Serializable]
    class AbstractMessage
    {
        public int Sequence { get; set; }
    }
}
