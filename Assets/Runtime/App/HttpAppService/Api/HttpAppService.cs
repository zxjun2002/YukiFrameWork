using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using MIKUFramework.IOC;
using System;
using Google.Protobuf;

namespace Yuki
{
    [Component]
    public partial class HttpAppService
    {
        #region 属性注入（网络应用服务注入全部放这里！！！）
        [Autowired] private IHttpModule httpModule;
        #endregion
        
        #region ProtoBuf序列化器
        /// <summary>序列化器 </summary>
        private JsonFormatter _jsonFormatter =  new(new JsonFormatter.Settings(true));
        public JsonFormatter JsonFormatter => _jsonFormatter;
        
        /// <summary> 反序列化器 </summary>
        private JsonParser _jsonParser = new(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));
        public JsonParser JsonParser => _jsonParser;
        #endregion
        
        #region 生命周期
        bool isFisrt = false; 
        
        AsyncReactiveProperty<bool> httplock = new AsyncReactiveProperty<bool>(false);
        bool oldHttpLock = false;
        IDisposable disposable;
        float time = .0f;

        public void Init()
        {
            Update();
            isFisrt = true;
            httplock.Subscribe(o => {
                //TODO:显示超时表现
            });
            RspHandlersInit();
        }

        void Update()
        {
            disposable = UniTaskAsyncEnumerable.EveryUpdate(PlayerLoopTiming.Update).Subscribe(v => {
                if (httpModule.GetisRequestInProgress()!= oldHttpLock) 
                {
                    oldHttpLock = httpModule.GetisRequestInProgress();
                    httplock.Value = oldHttpLock;
                }
            });
        }

        public void OnDestroy()
        {
            httplock?.Dispose();
            disposable?.Dispose();
        }
        #endregion
    }
}