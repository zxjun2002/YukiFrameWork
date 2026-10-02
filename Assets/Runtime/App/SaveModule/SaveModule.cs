using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MIKUFramework.IOC;
using UnityEngine;

namespace Yuki.SaveSystem
{
    [Component(typeof(ISaveModule))]
    public sealed class SaveModule : ISaveModule
    {
        private readonly object _sync = new object();
        private readonly Dictionary<string, SaveDatabase> _databases =
            new Dictionary<string, SaveDatabase>(StringComparer.OrdinalIgnoreCase);
        private string _directory;
        private bool _listeningForQuit;

        public void Init(string saveDirectory = null)
        {
            lock (_sync)
            {
                if (_databases.Values.Any(database => database.IsOpen))
                    throw new InvalidOperationException("请先关闭所有存档，再修改存档目录。");
                _directory = Path.GetFullPath(saveDirectory ??
                    Path.Combine(Application.persistentDataPath, "Saves"));
                Directory.CreateDirectory(_directory);
            }
        }

        public SaveDatabase Open(string slotName, string password)
        {
            SqlCipherSupport.ValidatePassword(password);
            return OpenCore(slotName, password);
        }

        public SaveDatabase OpenPlain(string slotName) => OpenCore(slotName, null);

        private SaveDatabase OpenCore(string slotName, string password)
        {
            lock (_sync)
            {
                string path = GetSavePath(slotName);
                if (_databases.TryGetValue(path, out var existing) && existing.IsOpen)
                    throw new InvalidOperationException("该存档已经打开，请复用当前对象或先关闭再打开。");
                var database = SaveDatabase.OpenFile(path, password);
                _databases[path] = database;
                if (!_listeningForQuit)
                {
                    Application.quitting += CloseAll;
                    _listeningForQuit = true;
                }
                return database;
            }
        }

        public string GetSavePath(string slotName)
        {
            if (string.IsNullOrEmpty(slotName) || slotName.Length > 64 ||
                !Regex.IsMatch(slotName, @"\A[A-Za-z0-9_-]+\z") ||
                Regex.IsMatch(slotName, @"\A(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])\z", RegexOptions.IgnoreCase))
                throw new ArgumentException("存档名限 1–64 个英文字母、数字、下划线或短横线，且不能使用系统保留名。", nameof(slotName));
            lock (_sync)
            {
                if (_directory == null) Init();
                return Path.Combine(_directory, slotName.ToLowerInvariant() + ".sqlite3");
            }
        }

        public void Close(string slotName)
        {
            lock (_sync)
            {
                string path = GetSavePath(slotName);
                if (_databases.TryGetValue(path, out var database))
                {
                    database.Dispose();
                    _databases.Remove(path);
                }
            }
        }

        public void CloseAll()
        {
            lock (_sync)
            {
                foreach (var database in _databases.Values) database.Dispose();
                _databases.Clear();
                if (_listeningForQuit)
                {
                    Application.quitting -= CloseAll;
                    _listeningForQuit = false;
                }
            }
        }

        public void Dispose() => CloseAll();
    }
}
