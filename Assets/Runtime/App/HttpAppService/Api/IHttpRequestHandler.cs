using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Google.Protobuf;

namespace Yuki
{
    public interface IHttpRequestHandler
    {
        GameReqType ReqType { get; }
        UniTask<List<GameBase_Rsp>> HandleRequest(IHttpModule httpModule, JsonFormatter jsonFormatter);
    }   
}