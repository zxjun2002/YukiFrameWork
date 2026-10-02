using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SQLite;

namespace Yuki.SaveSystem
{
    public sealed class SaveDatabase : IDisposable
    {
        private const string InfoTableName = "__yuki_save_info";
        private const SQLite3.Result NotADatabase = (SQLite3.Result)26; // SQLITE_NOTADB
        private readonly object _sync = new object();
        private readonly Dictionary<string, Type> _tableTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        private SQLiteConnection _connection;

        public string FilePath { get; }
        public bool IsEncrypted { get; }
        public bool IsOpen { get { lock (_sync) return _connection != null; } }

        private SaveDatabase(string filePath, bool isEncrypted, SQLiteConnection connection)
        {
            FilePath = filePath;
            IsEncrypted = isEncrypted;
            _connection = connection;
        }

        internal static SaveDatabase OpenFile(string filePath, string password)
        {
            SQLiteConnection connection = null;
            try
            {
                connection = new SQLiteConnection(filePath,
                    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
                if (password != null) SqlCipherSupport.ApplyKey(connection, password);
                // key 设置成功不代表密码正确；必须先读取 schema，失败时不能创建表或重建文件。
                connection.ExecuteScalar<int>("SELECT count(*) FROM sqlite_master");
                connection.Execute("PRAGMA temp_store = MEMORY");
                connection.Execute("PRAGMA foreign_keys = ON");
                connection.Execute("PRAGMA synchronous = FULL");
                connection.ExecuteScalar<string>("PRAGMA journal_mode = DELETE");
                connection.RunInTransaction(() =>
                {
                    connection.CreateTable<SaveInfo>();
                    var info = connection.Find<SaveInfo>(1);
                    if (info == null) connection.Insert(new SaveInfo { Id = 1, FormatVersion = 1 });
                    else if (info.FormatVersion != 1)
                        throw new SaveOpenException("不支持此存档格式版本。");
                });
                return new SaveDatabase(filePath, password != null, connection);
            }
            catch (SQLiteException exception)
            {
                connection?.Dispose();
                if (exception.Result == NotADatabase || exception.Result == SQLite3.Result.Corrupt)
                    throw new SaveOpenException("无法打开存档：密码错误、普通/加密模式不匹配或文件已损坏。原文件已保留。", exception);
                throw;
            }
            catch
            {
                connection?.Dispose();
                throw;
            }
        }

        /// <summary>按模型自动创建表或补充新增列；不会删除旧列。</summary>
        public SaveTable<T> Table<T>() where T : class, new()
        {
            return Use(connection =>
            {
                var mapping = connection.GetMapping<T>();
                ValidateMapping(mapping);
                if (_tableTypes.TryGetValue(mapping.TableName, out var modelType) && modelType != typeof(T))
                    throw new InvalidOperationException("同一个连接中，同一张表只能绑定一种存档模型。");
                // 每次检查实际 schema，避免事务回滚建表后留下错误的缓存。
                if (connection.IsInTransaction) connection.CreateTable<T>();
                else connection.RunInTransaction(() => connection.CreateTable<T>());
                _tableTypes[mapping.TableName] = typeof(T);
                return new SaveTable<T>(this, mapping);
            });
        }

        private static void ValidateMapping(TableMapping mapping)
        {
            if (mapping.TableName.Equals(InfoTableName, StringComparison.OrdinalIgnoreCase) ||
                mapping.TableName.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("此表名保留给存档系统使用。");
            ValidateIdentifier(mapping.TableName);
            if (mapping.Columns.Count(column => column.IsPK) != 1)
                throw new ArgumentException("存档模型必须有且仅有一个 [PrimaryKey] 公共属性。");
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in mapping.Columns)
            {
                ValidateIdentifier(column.Name);
                if (!names.Add(column.Name)) throw new ArgumentException("存档模型包含重复列名。");
                foreach (var index in column.Indices)
                    if (index.Name != null) ValidateIdentifier(index.Name);
            }
        }

        private static void ValidateIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name) || !Regex.IsMatch(name, @"\A[\p{L}_][\p{L}\p{Nd}_]*\z"))
                throw new ArgumentException("表名、列名和索引名只能包含文字、数字、下划线，且不能以数字开头。");
        }

        /// <summary>多张表一起提交；异常时全部回滚。请勿在回调中 await。</summary>
        public void Transaction(Action<SaveDatabase> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            Use(connection =>
            {
                if (connection.IsInTransaction) throw new InvalidOperationException("不支持嵌套 Transaction。");
                connection.RunInTransaction(() => action(this));
                return 0;
            });
        }

        public void ChangePassword(string newPassword)
        {
            SqlCipherSupport.ValidatePassword(newPassword);
            Use(connection =>
            {
                if (!IsEncrypted) throw new InvalidOperationException("普通存档不能直接改密码；请新建加密存档后迁移数据。");
                if (connection.IsInTransaction) throw new InvalidOperationException("不能在事务中修改存档密码。");
                SqlCipherSupport.ChangeKey(connection, newPassword);
                return 0;
            });
        }

        internal TResult Use<TResult>(Func<SQLiteConnection, TResult> action)
        {
            lock (_sync)
            {
                if (_connection == null) throw new ObjectDisposedException(nameof(SaveDatabase));
                return action(_connection);
            }
        }

        internal int Write(Func<SQLiteConnection, int> action)
        {
            return Use(connection =>
            {
                if (connection.IsInTransaction) return action(connection);
                int result = 0;
                connection.RunInTransaction(() => result = action(connection));
                return result;
            });
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_connection == null) return;
                if (_connection.IsInTransaction)
                    throw new InvalidOperationException("不能在事务回调中关闭存档。");
                _connection.Dispose();
                _connection = null;
            }
        }

        [UnityEngine.Scripting.Preserve, Table(InfoTableName)]
        private sealed class SaveInfo
        {
            [PrimaryKey] public int Id { get; set; }
            public int FormatVersion { get; set; }
            public SaveInfo() { }
        }
    }
}
