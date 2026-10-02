# SQLite / SQLCipher 存档

`SaveModule` 是数据库管理器，通过框架 IOC 获取。默认目录为 `Application.persistentDataPath/Saves`，也可以在第一次打开前配置目录。密码在打开加密存档时传入，不保存在配置文件中。

```csharp
using MIKUFramework.IOC;
using Yuki.SaveSystem;

var manager = IoCHelper.Instance.GetBean<ISaveModule>();
manager.Init(saveDirectory); // 可省略，使用默认目录

using (var database = manager.Open("slot_1", password))
{
    var table = database.Table<YourTable>();
    table.Save(yourData);
    var data = table.Find(primaryKey); // 不存在时返回 null
    var rows = table.All();
    table.Delete(primaryKey);
}
```

`Open("slot_1", password)` 打开或创建加密存档，密码不能为空；`OpenPlain("slot_1")` 打开普通 SQLite 存档。文件名为 `slot_1.sqlite3`，存档名限英文字母、数字、下划线和短横线。同一存档复用当前连接，或关闭后再打开。

Table 用 C# 类定义，通过 `SQLite` 的 `[Table("表名")]` 指定表名，使用公共 `{ get; set; }` 属性定义列。模型必须有公共无参构造函数和一个 `[PrimaryKey]` 属性；数字主键可加 `[AutoIncrement]`，查询列可加 `[Indexed]`，不存储的属性加 `[Ignore]`。

- `Table<T>()`：自动建表并补充新增列，保留旧数据和旧列。
- `Insert(value)`：插入，自增主键回填到对象。
- `Save(value)` / `SaveAll(values)`：按主键保存或批量保存；自增主键为 0 时插入，非零时更新已有行。
- `Find(key)` / `All()` / `Where(condition)` / `Count(condition)`：读取和条件查询。
- `Delete(key)` / `DeleteWhere(condition)`：删除。
- `Transaction(db => { ... })`：多表操作一起提交，异常时回滚；回调内不要使用 `async`、嵌套事务或关闭连接。
- `ChangePassword(newPassword)`：修改加密存档密码，不能在事务中调用。

密码错误、模式不匹配或文件损坏时抛出 `SaveOpenException`，保留原文件。路径、磁盘或锁冲突错误保留原始异常。关闭连接可使用 `using`、`manager.Close(slotName)` 或 `manager.CloseAll()`。

接口为同步操作。数据对象修改后需要再次 `Save` 才会落盘；事务回滚不恢复 C# 对象属性或已回填的自增 Id。复杂对象和集合需要拆列或自行序列化。自动补列不处理字段类型、主键变更及删列；重命名属性可用 `[Column("原列名")]` 保持旧档兼容。

底层依赖随项目保存在 `Packages/com.gilzoide.sqlite-net`，版本为 1.3.2，使用 SQLite3 Multiple Ciphers 的 `sqlcipher` / `legacy = 4` 配置，生成 SQLCipher 4 兼容格式。包含 Windows、Android、iOS 接入文件及许可证；密码通过原生 key API 传入，业务数据正常读写，文件在磁盘上加密。外部工具需支持 SQLCipher 4。

模型请标注 `[Table]`，构建时会自动保留模型和基类成员，避免 IL2CPP 裁剪。独立 asmdef 需引用 `Runtime` 和 `Gilzoide.SqliteNet`。Android/iOS 尚需在对应构建环境和设备验证。
