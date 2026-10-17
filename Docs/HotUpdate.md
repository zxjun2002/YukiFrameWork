# HybridCLR + Addressables 两场景热更新

以 [hy-brid-test](https://gitee.com/zxjun2002/hy-brid-test) 已跑通的实现为基础，适配本框架的 IoC、UI、配置表和构建流程。使用当前项目的 Unity 2022.3.62f2、Addressables 1.22.3 及已锁定的 HybridCLR 包，不替换依赖版本。

运行时只有两个场景：`Assets/Scenes/Bootstrap.unity` 是打进主包的首场景，负责更新、加载 DLL，然后加载热更场景；`Assets/Scenes/SampleScene.unity` 是通过 Addressables 地址 `Scenes/Main` 加载的热更场景。该地址是资源别名，不是另一个场景文件。业务入口 `GameSettingMgr` 在热更场景里初始化 IoC 和业务服务。

场景、分组和设置已随工程提供，打开首场景即可运行。无需执行额外的“配置框架接入”步骤；构建工具会自动维护必要的场景与资源配置。

## 程序集边界

| 程序集 | 内容 | 发布方式 |
| --- | --- | --- |
| Main | 启动器、IoC、事件中心、HTTP、存档、日志、行为树、UI 管理器、列表、屏幕适配 | IL2CPP 主包 |
| Config | MemoryPack 配置模型、表结构和访问器 | 热更 DLL |
| Runtime | GameSettingMgr、Domain、业务 UI、请求响应、协议、ConfigTable | 热更 DLL |

通用目录通过 `Main.asmref` 归入 Main，保留源文件路径及 GUID。Main 不引用 Config 或 Runtime；热更程序集按 `Config.dll → Runtime.dll` 的实际依赖顺序加载。业务事件枚举位于 GameSetting，事件中心接收 `Enum`，避免反向依赖。HTTP 的泛型响应不再约束为业务的 `GameBase_Rsp`。

启动顺序：Bootstrap → 初始化 Addressables → 检查/更新 catalog → 按 hotfix、aot、content 标签补齐依赖下载 → 读取清单 → 补 AOT 元数据 → 依次加载热更 DLL → 调用热更运行时初始化方法 → 加载 SampleScene → GameSettingMgr.Awake 初始化 IoC → 启动业务。

`RuntimeInitializeOnLoadMethod` 在 Player 中由加载器按 Unity 阶段和 `HotfixInitOrder` 调用；AfterSceneLoad 在业务场景加载后调用。这也包含 MemoryPack 自动生成的格式化器注册。编辑器复用已编译程序集，由 Unity 调用初始化方法，避免重复加载 DLL 和类型身份冲突。GameSettingMgr 不再在 BeforeSceneLoad 提前创建 IoC。

## 编辑器运行

1. 打开工程，等待脚本编译完成。
2. 打开 `Assets/Scenes/Bootstrap.unity`，点击 Play，自动加载 SampleScene 并启动业务。
3. 也可单独 Play SampleScene 调试业务；框架初始化在热更场景自己的 GameSettingMgr.Awake 中执行。

## AB 包加载位置

AB 由 Addressables 的 AssetBundleProvider 加载。项目中的入口如下：

| 位置 | 调用 | 作用 |
| --- | --- | --- |
| HotUpdateLoader.DownloadAsync | DownloadDependenciesAsync | 下载并缓存热更代码和内容依赖的 AB |
| HotUpdateLoader.LoadMetadataAsync / LoadAssembliesAsync | LoadAssetsAsync\<TextAsset\> | 从 AB 读取 AOT 元数据和热更 DLL 的 `.bytes` 资源 |
| HotUpdateLoader.RunAsync | LoadSceneAsync(settings.mainSceneAddress) | 加载地址为 Scenes/Main 的 SampleScene 及其依赖 AB |
| UIManager.OpenPanelAsync | LoadAssetAsync\<GameObject\> | 从 AB 读取 Panel/AUIPanel 等 UI 预制体 |
| ConfigTable.InitAsync | LoadAssetAsync\<TextAsset\> | 从 AB 读取 Config/ConfData 配置表 |

DLL 字节读取完成后由 `Assembly.Load` 加载成程序集。`DownloadDependenciesAsync` 完成不代表已经进入热更场景，切换场景发生在后面的 `LoadSceneAsync`。

如果编辑器的 Addressables Play Mode Script 选择 Use Asset Database，场景和资源会直接从工程读取。要验证真实 AB 读取，可先构建 Addressables 再选择 Use Existing Build，或在 Player 中验证。编辑器仍会复用 Unity 已编译的热更程序集。

配置表通过地址 `Config/ConfData` 加载，替代依赖工程目录的磁盘读取。旧的 `IConfigTable.Init(path)` 仍保留给编辑器工具和文件输入。

原 `Assets/Runtime/Resources` 预制体已迁至 `Assets/HotUpdateAssets/Prefabs`，GUID 不变。业务 UI 必须先使用 `await uiManager.OpenPanelAsync<AUIPanel>()`；已缓存界面可继续使用同步 `OpenPanel<T>()`。面板缓存持有 Addressables 句柄，在容器释放时关闭面板并释放句柄。

## 首次主包

1. 在 HybridCLR Installer 中确认本机 IL2CPP 已安装；切换目标平台，并在 Player Settings 选择 IL2CPP。
2. 在 Addressables Profiles 设置 Remote.BuildPath 和 Remote.LoadPath。初始开发地址是 `http://127.0.0.1:8000/[BuildTarget]`，仅适用于本机桌面验证；真机改用可访问的局域网地址或 CDN。正式远程根目录应带主包版本，例如 `https://cdn.example/game/1.0/[BuildTarget]`。
3. 运行 `Yuki/热更新/生成主包 DLL 与 AOT 元数据`。工具执行 HybridCLR Generate All，从当前平台的 HotUpdateDlls 和 AssembliesPostIl2CppStrip 导出 `.dll.bytes`，并根据生成的 AOTGenericReferences 列表生成清单。完整裁剪 AOT DLL 另存于 `HybridCLRData/HotUpdateBaselines/<平台>`，防止后续构建覆盖临时裁剪输出。
4. 等 Unity 完成生成代码的导入和编译，运行 `Yuki/热更新/构建 Addressables 资源`。
5. 备份本次的 `addressables_content_state.bin`、`HybridCLRData/HotUpdateBaselines/<平台>` 完整目录、AOT 导出目录、热更清单和远程路径设置，标明平台及主包版本。
6. 构建 Player。Build Settings 仅包含 Bootstrap，SampleScene 在 Addressables 中。构建检查会阻止错误的程序集边界、平台、清单或非 IL2CPP 配置。
7. 将 ServerData 对应平台的产物完整上传到配置的远程地址，再启动安装包验证。

本机可用 `python -m http.server 8000 --directory ServerData` 提供远程资源。AOTDll 组使用主包本地路径，并设为静态内容；HotUpdateDll 与 HotUpdateContent 使用远程路径。所有 DLL 均先复制为 `.bytes` 再导入 Unity，不会把原始 DLL 当成插件。

## 给已有主包发布补丁

1. 切换到与主包相同的平台，恢复该主包的 AOT 导出基线、清单及 Profiles 设置。
2. 修改 Runtime、Config 或 Addressables 业务资源。
3. 运行 `Yuki/热更新/编译并导出热更补丁 DLL`。此步骤保留主包的 AOT 元数据，不重新 Generate All，并检查补丁是否引用了基线中被裁剪的 AOT 类型或成员。
4. 运行 `Yuki/热更新/基于主包内容状态构建补丁`，选择该主包备份的 `addressables_content_state.bin`。工具沿用旧 catalog 版本；静态 AOT 资源发生变化时会阻止发布。
5. 将补丁远程产物部署到该主包版本的远程路径，保留仍被旧 catalog 引用的 bundle，重新启动旧包验证更新。

“构建 Addressables 资源”用于新主包；给已有主包更新应使用“基于主包内容状态构建补丁”。平台间的裁剪 AOT DLL 不可混用。修改 Main、原生插件、Unity/HybridCLR 版本，或新代码需要基线中不存在的 AOT 类型/泛型能力时，需要重新评估并重建主包。运行中的 CLR 程序集无法卸载，更新在下次启动时生效；DLL 加载之后发生错误时，启动器提示重启，下载阶段失败可重试。

## 验证

EditMode 测试位于 `Assets/Tests/Editor/HotUpdateTests.cs`，覆盖程序集边界、清单校验、IoC 扫描范围及缺失依赖、初始化阶段及顺序。上线前用真实 IL2CPP 安装包验证首次启动、缓存启动、旧包更新、断网后重试及目标平台的存档功能。

本次接入已在隔离工程中验证：Unity 编译、11 项 EditMode 测试、Bootstrap 至配置表/存档/AUIPanel 的编辑器完整启动、主包 DLL/AOT 导出及基线快照、Addressables 完整资源构建、补丁 DLL 的 AOT 兼容检查和基于原内容状态的补丁构建，均通过。

Windows IL2CPP 已成功导出 Visual Studio 工程。直接生成并运行 `.exe` 的验证尚未完成：本机 Unity 未检测到可用的 Windows C++ 编译工具链。补齐构建环境后，还需执行上述 Player 端验证；Android、iOS 等平台也需要各自生成基线并在设备上验证。

HybridCLRData、导出的 Code 目录及生成的 HybridCLR 文件属于平台构建产物，已加入忽略规则；干净检出后按上述步骤重新生成。
