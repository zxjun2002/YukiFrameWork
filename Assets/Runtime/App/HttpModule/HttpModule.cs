using System;
using Domain;
using MIKUFramework.IOC;
using UnityEngine;

namespace Yuki
{
    [Component]
    public partial class HttpModule : IHttpModule
    {
        private string Url = string.Empty;
        
        public void Init(string url)
        {
            Url = url;
        }
    }
    
    public enum HTTPVerbs
    {
        Get,
        Post,
        Put,
    }

    [Serializable]
    public class HttpInfo
    {
        public int code;
        public string info;
    }

    public enum HttpCode :int
    {
        /// <summary>连接成功</summary>
        success = 200,
        /// <summary>错误提示</summary>
        error = 500,
        /// <summary>踢出游戏</summary>
        kick = 403,
        /// <summary>需要更新</summary>
        update = 502
    }
}