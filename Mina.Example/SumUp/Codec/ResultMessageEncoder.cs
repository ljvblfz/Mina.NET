using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Example.SumUp.Message;

namespace Mina.Example.SumUp.Codec
{
    class ResultMessageEncoder<T> : AbstractMessageEncoder<T>
        where T : ResultMessage
    {
        public ResultMessageEncoder()
            : base(Constants.Result)
        { }

        protected override void EncodeBody(IOSession session, T message, IOBuffer output)
        {
            if (message.Ok)
            {
                output.PutInt16((short)Constants.ResultOk);
                output.PutInt32(message.Value);
            }
            else
            {
                output.PutInt16((short)Constants.ResultError);
            }
        }
    }
}
