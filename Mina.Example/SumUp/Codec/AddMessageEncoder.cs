using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Example.SumUp.Message;

namespace Mina.Example.SumUp.Codec
{
    class AddMessageEncoder<T> : AbstractMessageEncoder<T>
        where T : AddMessage
    {
        public AddMessageEncoder()
            : base(Constants.Add)
        { }

        protected override void EncodeBody(IOSession session, T message, IOBuffer output)
        {
            output.PutInt32(message.Value);
        }
    }
}
