using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using System.Threading;
using Yuki;

public interface IHttpModule 
{
    public long GetServerTimeAt();

    public bool GetisRequestInProgress();
    
    public void Init(string url);
    
    public UniTask<T> Request_Put<T>(int gameReqType, string json) where T : List<GameBase_Rsp>;
    
    public UniTask<T> Request_Put_Async<T>(int gameReqType, string json) where T : List<GameBase_Rsp>;

    public UniTask<T> Request_Post<T>(string path, string json, Dictionary<string, string> headers = null);
    
    public UniTask<T> Request_Post_Async<T>(string path, string json, Dictionary<string, string> headers = null);
}