using System.IO;
using MIKUFramework.IOC;
using UnityEngine;

namespace Yuki.SaveSystem
{
    [Component(typeof(ISaveModule))]
    public sealed class SaveModule : ISaveModule
    {
        private readonly object _sync = new object();
        private SaveDatabase _database;

        public SaveDatabase Open()
        {
            lock (_sync)
            {
                if (_database != null && _database.IsOpen) return _database;
                string path = GetSavePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                _database = SaveDatabase.OpenFile(path);
                Application.quitting -= Close;
                Application.quitting += Close;
                return _database;
            }
        }

        public string GetSavePath() => Path.Combine(Application.persistentDataPath, "Saves", "Yuki.sqlite3");

        public void Close()
        {
            lock (_sync)
            {
                _database?.Dispose();
                _database = null;
                Application.quitting -= Close;
            }
        }

        public void Dispose() => Close();
    }
}
