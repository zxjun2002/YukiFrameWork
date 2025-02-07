using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Yuki
{
    public partial class HttpAppService
    {
        public async UniTask SendHttpReq(IHttpRequestHandler handler)
        {
            await ProcessRequest(handler, handler.ReqType);
        }
    
        /// <summary>
        /// 通用的请求处理方法
        /// </summary>
        private async UniTask ProcessRequest<THandler>(THandler handler, GameReqType reqType)where THandler : IHttpRequestHandler
        {
            var response = await handler.HandleRequest(httpModule);

            if (response != null)
            {
                // 假设 GameResponse 只支持 List<GameBase_Rsp> 类型，我们在这里进行类型检查
                if (response is List<GameBase_Rsp> rspList)
                {
                    NewGameResponse(rspList, reqType);
                }
            }
            else
            {
                GameLogger.LogWarning($"请求失败: {reqType}，未返回有效数据");
            }
        }
    }   
}