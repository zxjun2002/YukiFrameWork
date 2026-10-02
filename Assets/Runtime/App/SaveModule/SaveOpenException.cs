using System;

namespace Yuki.SaveSystem
{
    public sealed class SaveOpenException : Exception
    {
        public SaveOpenException(string message, Exception innerException = null) : base(message, innerException) { }
    }
}
