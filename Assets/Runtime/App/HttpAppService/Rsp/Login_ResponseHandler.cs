using Domain;
using Google.Protobuf;
using MIKUFramework.IOC;

namespace Yuki
{
    public class Login_ResponseHandler : IResponseHandler<GameBase_Rsp>
    {
        public Login_ResponseHandler()
        {
            IoCHelper.Instance.Inject(this);
        }

        public void HandleResponse(GameBase_Rsp responseData,  JsonParser jsonParser)
        {
            GameLogger.LogWithColor($"[HttpAppService][Rsp]{responseData.api_id}: 登录数据：{responseData.data}", "#00F3FF");
            var ad_data = jsonParser.Parse<AccountPb.LoginRsp>(responseData.data.ToString());
        }
    }
}