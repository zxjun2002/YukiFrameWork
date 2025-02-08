public enum ProfileState
{
    Test,
}

public class GameConfig
{
    public static ProfileState ProfileState = ProfileState.Test;
}


public class HttpConfig
{
    public static string GameUrl
    {
        get
        {
            switch (GameConfig.ProfileState)
            {
                case ProfileState.Test:
                    return "http://localhost:8000";
            }
            return string.Empty;
        }
    }

    public const float Polling = 10f;//轮询
    public const float HeartBeat = 120f; //心跳
    public const int RequestTimeout = 3000; // 请求超时时间（毫秒）
    public const int DefaultRetryInterval = 4000; // 默认重试间隔（毫秒）
    public const int DefaultMaxRetries = 3; // 默认最大重试次数

    public const int TipTime = 2000; //错误弹窗时间
}