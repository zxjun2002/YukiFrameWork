using System;

namespace Yuki.SaveSystem
{
    public interface ISaveModule : IDisposable
    {
        /// <summary>使用内置密码打开 Yuki.sqlite3；重复调用复用当前连接。</summary>
        SaveDatabase Open();
        string GetSavePath();
        void Close();
    }
}
