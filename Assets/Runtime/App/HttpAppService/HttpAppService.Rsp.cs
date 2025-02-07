using System.Collections.Generic;

namespace Yuki
{
    public partial class HttpAppService
    {
        private void RspHandlersInit()
        {
            // 使用策略模式将请求类型映射到对应的响应处理类
            responseHandlers = new Dictionary<GameRspType, IResponseHandler<GameBase_Rsp>>
            {
                // { GameRspType.Login, new Login_ResponseHandler() },
                // TODO:添加其他响应类型的处理器
            };
        }
        
        private Dictionary<GameRspType, IResponseHandler<GameBase_Rsp>> responseHandlers;

        private IResponseHandler<GameBase_Rsp> GetHandler(GameRspType rspType, GameReqType reqType, GameBase_Rsp data)
        {
            if (responseHandlers.TryGetValue(rspType, out var handler))
            {
                return handler;
            }
            if ((int)reqType == (int)rspType)
            {
                GameLogger.LogWithColor($"[HttpAppService][Rsp]接口{(int)rspType} => 接收成功数据：{data}", "#00F3FF");
                return null;
            }
            GameLogger.LogWarning($"[HttpAppService][Rsp]{(int)rspType}: 收到未解析数据：{data},请求接口是：{reqType}");
            return null;
        }

        private void NewGameResponse(List<GameBase_Rsp> responseData, GameReqType reqType)
        {
            if (responseData == null || responseData.Count == 0)
            {
                GameLogger.LogError($"请求 {reqType} 返回空数据");
                return;
            }

            foreach (var rspData in responseData)
            {
                // 获取对应类型的处理器，并执行处理
                var handler = GetHandler((GameRspType)rspData.api_id, reqType, rspData);
                handler?.HandleResponse(rspData);
            }
        }
    }
}