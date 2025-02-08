using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Google.Protobuf;

namespace Yuki
{
    /// <summary>
    /// 登录
    /// req id = 1000
    /// </summary>
    public class Login_RequestHandler : IHttpRequestHandler
    {
        public  GameReqType ReqType { get; }
        public readonly AccountPb.LoginReq dataReq;

        public Login_RequestHandler(long pid, string name)
        {
            ReqType = GameReqType.LoginPlayer;
            dataReq = new AccountPb.LoginReq
            {
                Pid = pid,
                Name = name,
            };
        }

        public async UniTask<List<GameBase_Rsp>> HandleRequest(IHttpModule httpModule, JsonFormatter jsonFormatter)
        {
            var rspData = await httpModule.Request_Put<List<GameBase_Rsp>>((int)ReqType,jsonFormatter.Format(dataReq));
            return rspData;
        }
    }
}