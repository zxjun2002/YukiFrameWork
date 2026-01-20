using System;

namespace MIKUFramework.IOC
{
    public interface IMIKULogger
    {
        void Info(string msg);
        void Warn(string msg);
        void Error(string msg);
    }

    public sealed class ConsoleLogger : IMIKULogger
    {
        public void Info(string msg) => Console.WriteLine(msg);
        public void Warn(string msg) => Console.WriteLine("[W] " + msg);
        public void Error(string msg) => Console.Error.WriteLine("[E] " + msg);
    }   
}