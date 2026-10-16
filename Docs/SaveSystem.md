# SQLCipher 存档

`SaveModule` 是数据库管理器，通过框架 IOC 获取。整个游戏固定使用 `Application.persistentDataPath/Saves/Yuki.sqlite3` 一份加密存档，所有 Table 都在这个文件里。路径、文件名和密码不需要业务代码传入。

```csharp
using MIKUFramework.IOC;
using Yuki.SaveSystem;

var manager = IoCHelper.Instance.GetBean<ISaveModule>();

var database = manager.Open();
var table = database.Table<YourTable>();
table.Save(yourData);
var data = table.Find(primaryKey); // 不存在时返回 null
var rows = table.All();
table.Delete(primaryKey);

// 另一张表仍然使用同一份 Yuki.sqlite3
var anotherTable = database.Table<AnotherTable>();

// 不再读写时关闭；退出应用时也会自动关闭。
manager.Close();
```

`Open()` 使用内置密码打开或创建 SQLCipher 4 兼容存档。重复调用会返回当前连接，关闭后再调用会重新打开同一个文件。`GetSavePath()` 返回实际路径。模块不提供自定义文件名、目录切换或运行时改密入口。

连接由管理器统一持有；`manager.Close()`、`manager.Dispose()` 或直接释放 `database` 都会关闭它，此前取得的 Table 也不能继续使用。

## 定义 Table

```csharp
using SQLite;

[Table("YourTable")]
public sealed class YourTable
{
    [PrimaryKey] public int Id { get; set; }
    public string Value { get; set; }
}
```

Table 用 C# 类定义，通过 `SQLite` 的 `[Table("表名")]` 指定表名，使用公共 `{ get; set; }` 属性定义列。模型必须有公共无参构造函数和一个 `[PrimaryKey]` 属性；数字主键可加 `[AutoIncrement]`，查询列可加 `[Indexed]`，不存储的属性加 `[Ignore]`。

- `Table<T>()`：自动建表并补充新增列，保留旧数据和旧列。
- `Insert(value)`：插入，自增主键回填到对象。
- `Save(value)` / `SaveAll(values)`：按主键保存或批量保存；自增主键为 0 时插入，非零时更新已有行。
- `Find(key)` / `All()` / `Where(condition)` / `Count(condition)`：读取和条件查询。
- `Delete(key)` / `DeleteWhere(condition)`：删除。
- `Transaction(db => { ... })`：多表操作一起提交，异常时回滚；回调内不要使用 `async`、嵌套事务或关闭连接。

密码不匹配、文件格式不兼容或文件损坏时抛出 `SaveOpenException`，保留原文件。路径、磁盘或锁冲突错误保留原始异常。已有其他名称的存档不会自动重命名或迁移。

## 内置密码

默认使用约定的密码。`Assets/Runtime/App/SaveModule/SavePassword.cs` 只保存 AES-256-CBC 加密后的密码、密钥和 IV，不包含密码明文。连接前还原为 UTF-8 字节，直接传给原生 `sqlite3_key`，随后清空该字节数组；不创建密码字符串或写入日志。

此方式用于避免密码直接出现在源码中，解密密钥也在客户端，逆向仍能提取。以后若更换内置密码，需要重新生成密文、密钥和 IV，并安排已有存档迁移，不能只替换密码配置。

接口为同步操作。数据对象修改后需要再次 `Save` 才会落盘；事务回滚不恢复 C# 对象属性或已回填的自增 Id。复杂对象和集合需要拆列或自行序列化。自动补列不处理字段类型、主键变更及删列；重命名属性可用 `[Column("原列名")]` 保持旧档兼容。

底层依赖随项目保存在 `Packages/com.gilzoide.sqlite-net`，版本为 1.3.2，使用 SQLite3 Multiple Ciphers 的 `sqlcipher` / `legacy = 4` 配置，生成 SQLCipher 4 兼容格式。包含 Windows、Android、iOS 接入文件及许可证；密码通过原生 key API 传入，业务数据正常读写，文件在磁盘上加密。外部工具需支持 SQLCipher 4。

模型请标注 `[Table]`，构建时会自动保留模型和基类成员，避免 IL2CPP 裁剪。独立 asmdef 需引用 `Runtime` 和 `Gilzoide.SqliteNet`。Android/iOS 尚需在对应构建环境和设备验证。
