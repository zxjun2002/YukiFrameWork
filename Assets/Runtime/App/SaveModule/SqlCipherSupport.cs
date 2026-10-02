using System;
using System.Runtime.InteropServices;
using System.Text;
using SQLite;

namespace Yuki.SaveSystem
{
    internal static class SqlCipherSupport
    {
        [DllImport(SQLite3.LibraryPath, EntryPoint = "sqlite3_key", CallingConvention = CallingConvention.Cdecl)]
        private static extern SQLite3.Result Key(IntPtr database, byte[] key, int length);

        [DllImport(SQLite3.LibraryPath, EntryPoint = "sqlite3_rekey", CallingConvention = CallingConvention.Cdecl)]
        private static extern SQLite3.Result ReKey(IntPtr database, byte[] key, int length);

        internal static void ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("加密存档必须提供非空密码；普通存档请使用 OpenPlain。", nameof(password));
        }

        internal static void ApplyKey(SQLiteConnection connection, string password)
        {
            // 普通 SQLite 会忽略不认识的 PRAGMA。验证返回值，防止静默创建明文存档。
            if (connection.ExecuteScalar<string>("PRAGMA cipher = 'sqlcipher'") != "sqlcipher" ||
                connection.ExecuteScalar<int>("PRAGMA legacy = 4") != 4)
                throw new NotSupportedException("当前 SQLite 原生库不支持 SQLCipher 4 格式。");
            UseKey(connection, password, false);
        }

        internal static void ChangeKey(SQLiteConnection connection, string password) => UseKey(connection, password, true);

        private static void UseKey(SQLiteConnection connection, string password, bool rekey)
        {
            ValidatePassword(password);
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            try
            {
                // 使用原生 key API，密码不会拼进 SQL 或查询日志。
                var result = rekey ? ReKey(connection.Handle, bytes, bytes.Length) : Key(connection.Handle, bytes, bytes.Length);
                if (result != SQLite3.Result.OK)
                    throw SQLiteException.New(result, rekey ? "修改存档密码失败。" : "设置存档密码失败。");
            }
            finally
            {
                Array.Clear(bytes, 0, bytes.Length);
            }
        }
    }
}
