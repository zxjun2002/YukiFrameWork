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

        public Login_RequestHandler(string ConfigVersion, string deviceId)
        {
            ReqType = GameReqType.LoginPlayer;
            dataReq = new AccountPb.LoginReq
            {
                Pid = "2",
                Name = "cxk",
            };
        }

        public async UniTask<List<GameBase_Rsp>> HandleRequest(IHttpModule httpModule, JsonFormatter jsonFormatter)
        {
            var rspData = await httpModule.Request_Put<List<GameBase_Rsp>>((int)ReqType,jsonFormatter.Format(dataReq));
            return rspData;
        }
    }
}