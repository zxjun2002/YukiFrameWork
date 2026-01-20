namespace MIKUFramework.IOC
{
    public sealed class UnityIocLogger : IMIKULogger
    {
        public void Info(string msg) => GameLogger.Log(msg);
        public void Warn(string msg) => GameLogger.LogWarning(msg);
        public void Error(string msg) => GameLogger.LogError(msg);
    }
}