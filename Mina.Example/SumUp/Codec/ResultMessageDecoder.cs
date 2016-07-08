using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Example.SumUp.Message;

namespace Mina.Example.SumUp.Codec
{
    class ResultMessageDecoder : AbstractMessageDecoder
    {
        private int _code;
        private bool _readCode;

        public ResultMessageDecoder()
            : base(Constants.Result)
        { }

        protected override AbstractMessage DecodeBody(IOSession session, IOBuffer input)
        {
            if (!_readCode)
            {
                if (input.Remaining < Constants.ResultCodeLen)
                {
                    return null; // Need more data.
                }

                _code = input.GetInt16();
                _readCode = true;
            }

            if (_code == Constants.ResultOk)
            {
                if (input.Remaining < Constants.ResultValueLen)
                {
                    return null;
                }

                var m = new ResultMessage();
                m.Ok = true;
                m.Value = input.GetInt32();
                _readCode = false;
                return m;
            }
            else
            {
                var m = new ResultMessage();
                m.Ok = false;
                _readCode = false;
                return m;
            }
        }
    }
}
