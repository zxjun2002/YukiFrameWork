using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using dnlib.DotNet;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.HotUpdate;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEngine;
using Yuki.HotUpdate;

namespace Yuki.Editor.HotUpdate
{
    public static class HotUpdateBuild
    {
        internal static bool GeneratingAot { get; private set; }

        [MenuItem("Yuki/热更新/生成主包 DLL 与 AOT 元数据")]
        public static void GenerateBaseContent()
        {
            HotUpdateSetup.Configure();
            ValidateAssemblyBoundary();
            ValidateBackend();
            try
            {
                GeneratingAot = true;
                PrebuildCommand.GenerateAll();
            }
            finally { GeneratingAot = false; }

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string generated = Path.Combine(Application.dataPath, SettingsUtil.HybridCLRSettings.outputAOTGenericReferenceFile);
            string text = File.ReadAllText(generated);
            int start = text.IndexOf("// {{ AOT assemblies", StringComparison.Ordinal);
            int end = start < 0 ? -1 : text.IndexOf("// }}", start, StringComparison.Ordinal);
            if (start < 0 || end < 0) throw new BuildFailedException("无法解析 HybridCLR 生成的 AOT 元数据列表。");
            string[] aot = Regex.Matches(text.Substring(start, end - start), "\"([^\"]+\\.dll)\"")
                .Cast<Match>().Select(match => match.Groups[1].Value).Distinct().OrderBy(name => name, StringComparer.Ordinal).ToArray();
            var manifest = new HotUpdateManifest
            {
                buildTarget = target.ToString(),
                hotUpdateAssemblies = GetOrderedHotAssemblies(target),
                aotAssemblies = aot
            };
            HotUpdateLoader.ValidateManifest(manifest);
            CaptureAotBaseline(target);
            CopyDlls(SettingsUtil.GetAssembliesPostIl2CppStripDir(target), HotUpdateSetup.AotDirectory, aot,
                HotUpdateSetup.AotGroup, HotUpdateSettings.AotLabel);
            ExportHotDlls(manifest);
            Debug.Log("[HotUpdate] 主包基线已导出。下一步构建 Addressables，再构建 IL2CPP Player。");
        }

        [MenuItem("Yuki/热更新/编译并导出热更补丁 DLL")]
        public static void CompileAndExportHotUpdate()
        {
            ValidateAssemblyBoundary();
            var manifest = ReadManifest();
            ValidateTarget(manifest);
            // 复用主包基线的裁剪 AOT 元数据，不在补丁构建时重新生成。
            ValidateFiles(HotUpdateSetup.AotDirectory, manifest.aotAssemblies);
            string stripped = GetAotBaselineDirectory(EditorUserBuildSettings.activeBuildTarget);
            if (!Directory.Exists(stripped) || manifest.aotAssemblies.Any(name => !File.Exists(Path.Combine(stripped, name))))
                throw new BuildFailedException("缺少主包的裁剪 AOT 基线，请恢复 HybridCLRData/HotUpdateBaselines 对应平台目录，或重新生成主包基线。");
            CompileDllCommand.CompileDll(EditorUserBuildSettings.activeBuildTarget);
            manifest.hotUpdateAssemblies = GetOrderedHotAssemblies(EditorUserBuildSettings.activeBuildTarget);
            var checker = new MissingMetadataChecker(stripped, SettingsUtil.HotUpdateAssemblyNamesExcludePreserved);
            foreach (string name in manifest.hotUpdateAssemblies)
                if (!checker.Check(Path.Combine(SettingsUtil.GetHotUpdateDllsOutputDirByTarget(EditorUserBuildSettings.activeBuildTarget), name)))
                    throw new BuildFailedException($"补丁 {name} 引用了主包裁剪后不存在的 AOT 元数据，需要重建主包。");
            ExportHotDlls(manifest);
            Debug.Log("[HotUpdate] 补丁 DLL 已导出，AOT 元数据保持主包基线。");
        }

        [MenuItem("Yuki/热更新/构建 Addressables 资源")]
        public static void BuildAddressables()
        {
            ValidateContent();
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error)) throw new BuildFailedException(result.Error);
            Debug.Log("[HotUpdate] Addressables 构建完成，请将 Remote.BuildPath 产物部署到配置的 Remote.LoadPath。");
        }

        [MenuItem("Yuki/热更新/基于主包内容状态构建补丁")]
        public static void BuildContentUpdate()
        {
            string state = EditorUtility.OpenFilePanel("选择对应主包的 addressables_content_state.bin", "", "bin");
            if (!string.IsNullOrEmpty(state)) BuildContentUpdate(state);
        }

        public static void BuildContentUpdate(string state)
        {
            ValidateContent();
            if (!File.Exists(state)) throw new BuildFailedException("主包内容状态文件不存在。");
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var modifiedStatic = ContentUpdateScript.GatherModifiedEntries(settings, state);
            if (modifiedStatic.Count > 0)
                throw new BuildFailedException("主包的静态资源或 AOT 元数据已变化，需重新构建主包，不能作为现有主包补丁发布。");
            var result = ContentUpdateScript.BuildContentUpdate(settings, state);
            if (result == null || !string.IsNullOrEmpty(result.Error)) throw new BuildFailedException(result?.Error ?? "补丁构建失败。");
            Debug.Log("[HotUpdate] 已沿用主包 catalog 版本生成补丁，可部署到同一主包版本的远程目录。");
        }

        public static void ValidateContent()
        {
            ValidateAssemblyBoundary();
            var manifest = ReadManifest();
            ValidateTarget(manifest);
            ValidateFiles(HotUpdateSetup.HotDirectory, manifest.hotUpdateAssemblies);
            ValidateFiles(HotUpdateSetup.AotDirectory, manifest.aotAssemblies);
            string[] expected = SettingsUtil.HotUpdateAssemblyFilesExcludePreserved.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            if (!expected.SequenceEqual(manifest.hotUpdateAssemblies.OrderBy(name => name, StringComparer.Ordinal)))
                throw new BuildFailedException("热更清单与 HybridCLR 的程序集配置不一致，请重新导出 DLL。");
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || !settings.BuildRemoteCatalog) throw new BuildFailedException("缺少 Addressables 配置或未启用 Remote Catalog。");
            var config = AssetDatabase.LoadAssetAtPath<HotUpdateSettings>(HotUpdateSettings.AssetPath);
            if (config == null) throw new BuildFailedException("缺少 HotUpdateSettings。");
            RequireEntry(settings, HotUpdateSetup.ManifestPath, HotUpdateSettings.ManifestAddress, HotUpdateSettings.HotfixLabel);
            RequireEntry(settings, HotUpdateSetup.MainScenePath, config.mainSceneAddress, HotUpdateSettings.ContentLabel);
            RequireEntry(settings, "Assets/ConfigData/Configs/ConfData.bytes", ResEditorConfig.ConfsAddress, HotUpdateSettings.ContentLabel);
            foreach (string name in manifest.hotUpdateAssemblies)
                RequireEntry(settings, HotUpdateSetup.HotDirectory + "/" + name + ".bytes", "HotUpdate/Hotfix/" + name, HotUpdateSettings.HotfixLabel);
            foreach (string name in manifest.aotAssemblies)
                RequireEntry(settings, HotUpdateSetup.AotDirectory + "/" + name + ".bytes", "HotUpdate/AOT/" + name, HotUpdateSettings.AotLabel);
        }

        public static void ValidateAssemblyBoundary()
        {
            var hot = new HashSet<string>(SettingsUtil.HotUpdateAssemblyNamesExcludePreserved);
            if (!hot.Contains("Config") || !hot.Contains("Runtime") || hot.Contains("Main"))
                throw new BuildFailedException("HybridCLR 应配置 Config、Runtime 为热更程序集，Main 保留在 AOT 主包。");
            foreach (var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player).Where(assembly => !hot.Contains(assembly.name)))
            foreach (var reference in assembly.assemblyReferences)
                if (hot.Contains(reference.name))
                    throw new BuildFailedException($"AOT 程序集 {assembly.name} 反向引用了热更程序集 {reference.name}。");
        }

        internal static void ValidateBackend()
        {
            var group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            if (PlayerSettings.GetScriptingBackend(group) != ScriptingImplementation.IL2CPP)
                throw new BuildFailedException("当前平台需要在 Player Settings 中选择 IL2CPP。");
        }

        private static void ExportHotDlls(HotUpdateManifest manifest)
        {
            HotUpdateLoader.ValidateManifest(manifest);
            CopyDlls(SettingsUtil.GetHotUpdateDllsOutputDirByTarget(EditorUserBuildSettings.activeBuildTarget),
                HotUpdateSetup.HotDirectory, manifest.hotUpdateAssemblies, HotUpdateSetup.HotGroup, HotUpdateSettings.HotfixLabel);
            File.WriteAllText(HotUpdateSetup.ManifestPath, JsonUtility.ToJson(manifest, true));
            AssetDatabase.ImportAsset(HotUpdateSetup.ManifestPath, ImportAssetOptions.ForceSynchronousImport);
            HotUpdateSetup.Register(AddressableAssetSettingsDefaultObject.Settings, HotUpdateSetup.ManifestPath,
                HotUpdateSetup.HotGroup, HotUpdateSettings.ManifestAddress, HotUpdateSettings.HotfixLabel);
            AssetDatabase.SaveAssets();
        }

        internal static string GetAotBaselineDirectory(BuildTarget target) =>
            Path.Combine(SettingsUtil.HybridCLRDataDir, "HotUpdateBaselines", target.ToString());

        private static void CaptureAotBaseline(BuildTarget target)
        {
            string source = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            string destination = GetAotBaselineDirectory(target);
            string[] files = Directory.GetFiles(source, "*.dll");
            if (files.Length == 0) throw new BuildFailedException("裁剪 AOT 输出为空，不能保存主包基线。");
            Directory.CreateDirectory(destination);
            var expected = new HashSet<string>(files.Select(Path.GetFileName), StringComparer.Ordinal);
            foreach (string stale in Directory.GetFiles(destination, "*.dll"))
                if (!expected.Contains(Path.GetFileName(stale))) File.Delete(stale);
            foreach (string file in files) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        }

        private static string[] GetOrderedHotAssemblies(BuildTarget target)
        {
            string directory = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            var names = new HashSet<string>(SettingsUtil.HotUpdateAssemblyNamesExcludePreserved, StringComparer.Ordinal);
            var dependencies = new Dictionary<string, string[]>();
            foreach (string name in names)
            {
                string path = Path.Combine(directory, name + ".dll");
                if (!File.Exists(path)) throw new BuildFailedException($"缺少编译后的热更程序集：{path}");
                using (var module = ModuleDefMD.Load(path))
                    dependencies[name] = module.GetAssemblyRefs().Select(reference => reference.Name.String)
                        .Where(names.Contains).ToArray();
            }
            var ordered = new List<string>();
            var pending = new SortedSet<string>(names, StringComparer.Ordinal);
            while (pending.Count > 0)
            {
                string ready = pending.FirstOrDefault(name => dependencies[name].All(dependency => !pending.Contains(dependency)));
                if (ready == null) throw new BuildFailedException("热更程序集之间存在循环依赖。");
                ordered.Add(ready + ".dll");
                pending.Remove(ready);
            }
            return ordered.ToArray();
        }

        private static void CopyDlls(string source, string destination, string[] names, string group, string label)
        {
            foreach (string name in names)
            {
                string path = Path.Combine(source, name);
                if (!File.Exists(path)) throw new BuildFailedException($"缺少 {path}，请先为当前平台执行 Generate All。");
                using (var module = ModuleDefMD.Load(path))
                    if (module.Assembly.Name.String + ".dll" != name) throw new BuildFailedException($"DLL 名称与内容不符：{path}");
            }
            Directory.CreateDirectory(destination);
            var expected = new HashSet<string>(names.Select(name => name + ".bytes"));
            foreach (string old in Directory.GetFiles(destination, "*.bytes"))
                if (!expected.Contains(Path.GetFileName(old))) AssetDatabase.DeleteAsset(old.Replace('\\', '/'));
            foreach (string name in names)
            {
                string path = destination + "/" + name + ".bytes";
                File.Copy(Path.Combine(source, name), path, true);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                HotUpdateSetup.Register(AddressableAssetSettingsDefaultObject.Settings, path, group, "HotUpdate/" +
                    (label == HotUpdateSettings.AotLabel ? "AOT/" : "Hotfix/") + name, label);
            }
        }

        private static HotUpdateManifest ReadManifest()
        {
            if (!File.Exists(HotUpdateSetup.ManifestPath)) throw new BuildFailedException("尚未生成主包基线，请先运行“生成主包 DLL 与 AOT 元数据”。");
            var manifest = JsonUtility.FromJson<HotUpdateManifest>(File.ReadAllText(HotUpdateSetup.ManifestPath));
            HotUpdateLoader.ValidateManifest(manifest);
            return manifest;
        }

        private static void ValidateTarget(HotUpdateManifest manifest)
        {
            if (manifest.buildTarget != EditorUserBuildSettings.activeBuildTarget.ToString())
                throw new BuildFailedException($"DLL 来自 {manifest.buildTarget}，当前平台是 {EditorUserBuildSettings.activeBuildTarget}，请重新生成。");
        }

        private static void ValidateFiles(string directory, string[] names)
        {
            foreach (string name in names)
                if (!File.Exists(directory + "/" + name + ".bytes")) throw new BuildFailedException($"缺少导出的 DLL：{name}");
        }

        private static void RequireEntry(AddressableAssetSettings settings, string path, string address, string label)
        {
            var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            if (entry == null || entry.address != address || !entry.labels.Contains(label))
                throw new BuildFailedException($"Addressables 分组、地址或标签不完整：{path}");
        }
    }
}
