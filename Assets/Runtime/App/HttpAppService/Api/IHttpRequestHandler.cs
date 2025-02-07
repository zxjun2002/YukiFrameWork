using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Yuki
{
    public interface IHttpRequestHandler
    {
        GameReqType ReqType { get; }
        UniTask<List<GameBase_Rsp>> HandleRequest(IHttpModule httpModule);
    }   
}