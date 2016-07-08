using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Example.SumUp.Message;

namespace Mina.Example.SumUp.Codec
{
    class AddMessageDecoder : AbstractMessageDecoder
    {
        public AddMessageDecoder()
            : base(Constants.Add)
        { }

        protected override AbstractMessage DecodeBody(IOSession session, IOBuffer input)
        {
            if (input.Remaining < Constants.AddBodyLen)
            {
                return null;
            }

            var m = new AddMessage();
            m.Value = input.GetInt32();
            return m;
        }
    }
}
