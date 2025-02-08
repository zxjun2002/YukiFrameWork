using System;

namespace Yuki
{
    public enum GameRspType
    {
        /// <summary> 登陆账户 </summary>
        Login = 1000, //登录
    }
    #region  Base
    [Serializable]
    public class GameBase_Rsp
    {
        public int api_id;
        public int typ;
        public object data;
    }
    #endregion   
}