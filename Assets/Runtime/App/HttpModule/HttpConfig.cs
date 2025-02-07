public enum ProfileState
{
    Develop,
    Release,
    PreRelease,
    Hongkong,
    Test,
    Bin,
    NewLogin
}

public class GameConfig
{
    public static ProfileState ProfileState = ProfileState.PreRelease;
}


public class HttpConfig
{
    public static string GameUrl
    {
        get
        {
            switch (GameConfig.ProfileState)
            {
                case ProfileState.Release:
                    return "https://mc-rogue.xmfunny.com";
                case ProfileState.PreRelease:
                    return "https://mc-rogue-pre.xmfunny.com";
                case ProfileState.Develop:
                    return "https://mc-rogue-dev.sofunny.io";
                case ProfileState.Test:
                    return "http://mc-rogue-cjs.local.com";	
                case ProfileState.Bin:
                    return "http://10.30.40.65";
                case ProfileState.NewLogin:
                    return "http://mc-rogue-game.xmfunny.com:8080";
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