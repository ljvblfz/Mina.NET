namespace Mina.Example.SumUp.Codec
{
    static class Constants
    {
        public static readonly int TypeLen = 2;

        public static readonly int SequenceLen = 4;

        public static readonly int HeaderLen = TypeLen + SequenceLen;

        public static readonly int BodyLen = 12;

        public static readonly int Result = 0;

        public static readonly int Add = 1;

        public static readonly int ResultCodeLen = 2;

        public static readonly int ResultValueLen = 4;

        public static readonly int AddBodyLen = 4;

        public static readonly int ResultOk = 0;

        public static readonly int ResultError = 1;
    }
}
