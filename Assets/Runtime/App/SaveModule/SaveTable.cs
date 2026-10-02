using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SQLite;

namespace Yuki.SaveSystem
{
    public sealed class SaveTable<T> where T : class, new()
    {
        private readonly SaveDatabase _database;
        private readonly TableMapping _mapping;
        public string Name => _mapping.TableName;

        internal SaveTable(SaveDatabase database, TableMapping mapping)
        {
            _database = database;
            _mapping = mapping;
        }

        /// <summary>插入新行；AutoIncrement 的主键会回填到对象。</summary>
        public int Insert(T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return _database.Write(connection => connection.Insert(value));
        }

        /// <summary>按主键更新；不存在时插入。自增主键为 0 时作为新行插入。</summary>
        public int Save(T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return _database.Write(connection => SaveCore(connection, value));
        }

        private int SaveCore(SQLiteConnection connection, T value)
        {
            object key = _mapping.PK.GetValue(value);
            if (key == null) throw new ArgumentException("主键不能为 null。", nameof(value));
            if (_mapping.HasAutoIncPK && Convert.ToInt64(key) == 0) return connection.Insert(value);
            int changed = connection.Update(value);
            if (changed != 0) return changed;
            if (_mapping.HasAutoIncPK)
                throw new InvalidOperationException("自增主键对应的行不存在；插入新行请把主键设为 0 或使用 Insert。");
            return connection.Insert(value);
        }

        /// <summary>批量保存；任何一条失败时整批回滚。</summary>
        public int SaveAll(IEnumerable<T> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            return _database.Write(connection =>
            {
                int count = 0;
                foreach (var value in values)
                {
                    if (value == null) throw new ArgumentException("批量存档不能包含 null。", nameof(values));
                    count += SaveCore(connection, value);
                }
                return count;
            });
        }

        public T Find(object primaryKey)
        {
            if (primaryKey == null) throw new ArgumentNullException(nameof(primaryKey));
            return _database.Use(connection => connection.Find<T>(primaryKey));
        }

        public List<T> All() => _database.Use(connection => connection.Table<T>().ToList());

        /// <summary>条件由 SQLite 执行；结果在连接锁内读取完毕。</summary>
        public List<T> Where(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return _database.Use(connection => connection.Table<T>().Where(predicate).ToList());
        }

        public int Count() => _database.Use(connection => connection.Table<T>().Count());

        public int Count(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return _database.Use(connection => connection.Table<T>().Count(predicate));
        }

        public int Delete(object primaryKey)
        {
            if (primaryKey == null) throw new ArgumentNullException(nameof(primaryKey));
            return _database.Write(connection => connection.Delete<T>(primaryKey));
        }

        public int DeleteWhere(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return _database.Write(connection => connection.Table<T>().Delete(predicate));
        }
    }
}
