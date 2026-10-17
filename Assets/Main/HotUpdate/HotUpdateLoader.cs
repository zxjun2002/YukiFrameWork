using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using HybridCLR;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Yuki.HotUpdate
{
    public sealed class HotUpdateLoader
    {
        public string Status { get; private set; } = "准备启动";
        public float Progress { get; private set; }
        public bool RequiresRestart { get; private set; }

        public async UniTask RunAsync(HotUpdateSettings settings, CancellationToken cancellationToken)
        {
            if (settings == null) throw new InvalidOperationException("未配置 HotUpdateSettings。");
#if !UNITY_EDITOR && !ENABLE_IL2CPP
            throw new NotSupportedException("热更 Player 必须使用 IL2CPP 构建。");
#else
            SetStatus("初始化资源系统");
            var initialize = Addressables.InitializeAsync(false);
            try { await initialize; }
            finally { if (initialize.IsValid()) Addressables.Release(initialize); }
            cancellationToken.ThrowIfCancellationRequested();

#if !UNITY_EDITOR
            if (settings.checkForCatalogUpdates) await UpdateCatalogsAsync();
            // 即使没有新 catalog，首次启动仍需要下载尚未缓存的必要资源。
            await DownloadAsync(cancellationToken);
#endif
            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_EDITOR
            SetStatus("使用编辑器程序集");
            foreach (string name in settings.editorAssemblyNames)
                if (!AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == name))
                    throw new InvalidOperationException($"编辑器中未找到热更程序集：{name}");
#else
            HotUpdateManifest manifest = await LoadManifestAsync();
            ValidateManifest(manifest);
            ValidatePlayerTarget(manifest);
            SetStatus("补充 AOT 元数据");
            RequiresRestart = true;
            await LoadMetadataAsync(manifest.aotAssemblies);
            SetStatus("加载热更程序集");
            List<Assembly> assemblies = await LoadAssembliesAsync(manifest.hotUpdateAssemblies);
            // Unity 不会自动调用动态加载程序集的这些方法；MemoryPack 注册也在这里执行。
            HotfixInitialization.Invoke(assemblies, false);
#endif
            cancellationToken.ThrowIfCancellationRequested();
            RequiresRestart = true;
            // 首场景只完成更新和加载；业务初始化由热更场景自己的入口负责。
            SetStatus("进入游戏");
            var scene = Addressables.LoadSceneAsync(settings.mainSceneAddress);
            try
            {
                await scene;
                if (scene.Status != AsyncOperationStatus.Succeeded)
                    throw new InvalidOperationException("业务场景加载失败。", scene.OperationException);
                // 成功的场景句柄由 Addressables 在 Single 场景切换时释放。
            }
            catch
            {
                if (scene.IsValid()) Addressables.Release(scene);
                throw;
            }
#if !UNITY_EDITOR
            HotfixInitialization.Invoke(assemblies, true);
#endif
            SetStatus("启动完成");
            Progress = 1;
#endif
        }

        private async UniTask UpdateCatalogsAsync()
        {
            SetStatus("检查资源更新");
            var check = Addressables.CheckForCatalogUpdates(false);
            List<string> catalogs;
            try { catalogs = new List<string>(await check); }
            finally { if (check.IsValid()) Addressables.Release(check); }
            if (catalogs.Count == 0) return;
            var update = Addressables.UpdateCatalogs(catalogs, false);
            try { await update; }
            finally { if (update.IsValid()) Addressables.Release(update); }
        }

        private async UniTask DownloadAsync(CancellationToken cancellationToken)
        {
            var labels = new object[] { HotUpdateSettings.HotfixLabel, HotUpdateSettings.AotLabel, HotUpdateSettings.ContentLabel };
            var sizeHandle = Addressables.GetDownloadSizeAsync((System.Collections.IEnumerable)labels);
            long size;
            try { size = await sizeHandle; }
            finally { if (sizeHandle.IsValid()) Addressables.Release(sizeHandle); }
            if (size == 0) return;
            SetStatus($"下载更新（{size / 1048576f:F1} MB）");
            var download = Addressables.DownloadDependenciesAsync((System.Collections.IEnumerable)labels, Addressables.MergeMode.Union, false);
            try
            {
                while (!download.IsDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Progress = download.GetDownloadStatus().Percent;
                    await UniTask.Yield();
                }
                await download;
            }
            finally { if (download.IsValid()) Addressables.Release(download); }
        }

        private static async UniTask<HotUpdateManifest> LoadManifestAsync()
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(HotUpdateSettings.ManifestAddress);
            try { return JsonUtility.FromJson<HotUpdateManifest>((await handle).text); }
            finally { if (handle.IsValid()) Addressables.Release(handle); }
        }

        public static void ValidateManifest(HotUpdateManifest manifest)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.buildTarget) ||
                manifest.hotUpdateAssemblies == null || manifest.hotUpdateAssemblies.Length == 0 || manifest.aotAssemblies == null)
                throw new InvalidOperationException("热更清单不完整，请重新导出 DLL。");
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in manifest.hotUpdateAssemblies.Concat(manifest.aotAssemblies))
                if (string.IsNullOrWhiteSpace(name) || !name.EndsWith(".dll", StringComparison.Ordinal) ||
                    name.Contains("/") || name.Contains("\\") || !names.Add(name))
                    throw new InvalidOperationException($"热更清单的程序集名称无效或重复：{name}");
            if (manifest.hotUpdateAssemblies.Contains("Main.dll"))
                throw new InvalidOperationException("Main.dll 必须留在 AOT 主包中。");
        }

        private static void ValidatePlayerTarget(HotUpdateManifest manifest)
        {
            string expected = null;
#if UNITY_STANDALONE_WIN
            expected = IntPtr.Size == 8 ? "StandaloneWindows64" : "StandaloneWindows";
#elif UNITY_ANDROID
            expected = "Android";
#elif UNITY_IOS
            expected = "iOS";
#elif UNITY_WEBGL
            expected = "WebGL";
#elif UNITY_STANDALONE_OSX
            expected = "StandaloneOSX";
#elif UNITY_STANDALONE_LINUX
            expected = "StandaloneLinux64";
#elif UNITY_WSA
            expected = "WSAPlayer";
#endif
            if (expected == null || manifest.buildTarget != expected)
                throw new InvalidOperationException($"热更资源平台 {manifest.buildTarget} 与当前 Player {expected ?? Application.platform.ToString()} 不一致。");
        }

        private static async UniTask LoadMetadataAsync(string[] names)
        {
            if (names.Length == 0) return;
            var handle = Addressables.LoadAssetsAsync<TextAsset>(HotUpdateSettings.AotLabel, null);
            try
            {
                var assets = IndexAssets(await handle, names);
                foreach (string name in names)
                {
                    var code = RuntimeApi.LoadMetadataForAOTAssembly(assets[name].bytes, HomologousImageMode.SuperSet);
                    if (code != LoadImageErrorCode.OK)
                        throw new InvalidOperationException($"补充 AOT 元数据失败：{name}，{code}");
                }
            }
            finally { if (handle.IsValid()) Addressables.Release(handle); }
        }

        private static async UniTask<List<Assembly>> LoadAssembliesAsync(string[] names)
        {
            var handle = Addressables.LoadAssetsAsync<TextAsset>(HotUpdateSettings.HotfixLabel, null);
            try
            {
                var assets = IndexAssets(await handle, names);
                var assemblies = new List<Assembly>();
                foreach (string name in names)
                {
                    if (AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name + ".dll" == name))
                        throw new InvalidOperationException($"程序集已被加载或错误地打入主包：{name}");
                    var loaded = Assembly.Load(assets[name].bytes);
                    if (loaded.GetName().Name + ".dll" != name)
                        throw new InvalidOperationException($"DLL 内容与文件名不一致：{name}");
                    assemblies.Add(loaded);
                }
                return assemblies;
            }
            finally { if (handle.IsValid()) Addressables.Release(handle); }
        }

        private static Dictionary<string, TextAsset> IndexAssets(IList<TextAsset> assets, string[] required)
        {
            var indexed = new Dictionary<string, TextAsset>(StringComparer.Ordinal);
            foreach (var asset in assets)
            {
                // Manifest 也使用 hotfix 标签，但不是 DLL。
                if (!asset.name.EndsWith(".dll", StringComparison.Ordinal)) continue;
                if (indexed.ContainsKey(asset.name)) throw new InvalidOperationException($"DLL 标签中存在同名资源：{asset.name}");
                indexed.Add(asset.name, asset);
            }
            foreach (string name in required)
                if (!indexed.ContainsKey(name)) throw new InvalidOperationException($"缺少 DLL 资源：{name}");
            return indexed;
        }

        private void SetStatus(string status)
        {
            Status = status;
            Progress = 0;
            Debug.Log($"[HotUpdate] {status}");
        }
    }
}
