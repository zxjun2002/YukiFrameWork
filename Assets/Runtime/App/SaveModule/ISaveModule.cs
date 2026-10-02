using System;

namespace Yuki.SaveSystem
{
    public interface ISaveModule : IDisposable
    {
        /// <summary>设置存档目录；省略时使用 persistentDataPath/Saves。</summary>
        void Init(string saveDirectory = null);
        /// <summary>打开或创建 SQLCipher 4 格式存档，密码不能为空。</summary>
        SaveDatabase Open(string slotName, string password);
        /// <summary>打开或创建普通 SQLite 存档。</summary>
        SaveDatabase OpenPlain(string slotName);
        string GetSavePath(string slotName);
        void Close(string slotName);
        void CloseAll();
    }
}
