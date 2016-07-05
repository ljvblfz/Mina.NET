using System;

namespace Mina.Example.SumUp.Message
{
    [Serializable]
    class AddMessage : AbstractMessage
    {
        public int Value { get; set; }

        public override string ToString()
        {
            return Sequence + ":ADD(" + Value + ')';
        }
    }
}
