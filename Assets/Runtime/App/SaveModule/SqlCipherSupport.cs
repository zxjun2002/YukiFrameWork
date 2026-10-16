using System;
using System.Runtime.InteropServices;
using SQLite;

namespace Yuki.SaveSystem
{
    internal static class SqlCipherSupport
    {
        [DllImport(SQLite3.LibraryPath, EntryPoint = "sqlite3_key", CallingConvention = CallingConvention.Cdecl)]
        private static extern SQLite3.Result Key(IntPtr database, byte[] key, int length);

        internal static void ApplyKey(SQLiteConnection connection)
        {
            // 普通 SQLite 会忽略不认识的 PRAGMA。验证返回值，防止静默创建明文存档。
            if (connection.ExecuteScalar<string>("PRAGMA cipher = 'sqlcipher'") != "sqlcipher" ||
                connection.ExecuteScalar<int>("PRAGMA legacy = 4") != 4)
                throw new NotSupportedException("当前 SQLite 原生库不支持 SQLCipher 4 格式。");
            byte[] bytes = SavePassword.Decrypt();
            try
            {
                if (bytes.Length == 0) throw new InvalidOperationException("内置存档密码不能为空。");
                // 使用原生 key API，密码不会拼进 SQL 或查询日志。
                var result = Key(connection.Handle, bytes, bytes.Length);
                if (result != SQLite3.Result.OK)
                    throw SQLiteException.New(result, "设置存档密码失败。");
            }
            finally
            {
                Array.Clear(bytes, 0, bytes.Length);
            }
        }
    }
}
