using System;

namespace Mina.Example.SumUp.Message
{
    [Serializable]
    class ResultMessage : AbstractMessage
    {
        public bool Ok { get; set; }

        public int Value { get; set; }

        public override string ToString()
        {
            return Sequence + (Ok ? ":RESULT(" + Value + ')' : ":RESULT(ERROR)");
        }
    }
}
