using System;
using System.IO;
using System.Linq;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yuki.HotUpdate;

namespace Yuki.Editor.HotUpdate
{
    public static class HotUpdateSetup
    {
        public const string CodeRoot = "Assets/HotUpdateAssets/Code";
        public const string HotDirectory = CodeRoot + "/Hotfix";
        public const string AotDirectory = CodeRoot + "/AOT";
        public const string ManifestPath = CodeRoot + "/manifest.json";
        public const string MainScenePath = "Assets/Scenes/SampleScene.unity";
        public const string PrefabRoot = "Assets/HotUpdateAssets/Prefabs";
        public const string HotGroup = "HotUpdateDll";
        public const string AotGroup = "AOTDll";
        public const string ContentGroup = "HotUpdateContent";

        // 构建时自动维护两场景所需的配置，无需额外的手动接入步骤。
        public static void Configure()
        {
            EnsureDirectory(HotDirectory);
            EnsureDirectory(AotDirectory);
            EnsureDirectory("Assets/Main/HybridCLRGenerate");
            AssetDatabase.Refresh();
            var config = AssetDatabase.LoadAssetAtPath<HotUpdateSettings>(HotUpdateSettings.AssetPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<HotUpdateSettings>();
                AssetDatabase.CreateAsset(config, HotUpdateSettings.AssetPath);
            }

            var hybrid = HybridCLRSettings.Instance;
            hybrid.enable = true;
            var definitions = (hybrid.hotUpdateAssemblyDefinitions ?? Array.Empty<AssemblyDefinitionAsset>()).ToList();
            foreach (string path in new[] { "Assets/ConfigData/Config.asmdef", "Assets/Runtime/Runtime.asmdef" })
            {
                var definition = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                if (!definitions.Contains(definition)) definitions.Add(definition);
            }
            hybrid.hotUpdateAssemblyDefinitions = definitions.ToArray();
            hybrid.outputLinkFile = "Main/HybridCLRGenerate/link.xml";
            hybrid.outputAOTGenericReferenceFile = "Main/HybridCLRGenerate/AOTGenericReferences.cs";
            HybridCLRSettings.Save();

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                settings = AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                    AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
                AddressableAssetSettingsDefaultObject.Settings = settings;
                // 开发环境默认地址。正式发布前在 Addressables Profiles 中设置带主包版本的 CDN 路径。
                settings.profileSettings.SetValue(settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath,
                    "http://127.0.0.1:8000/[BuildTarget]");
            }
            foreach (string label in new[] { HotUpdateSettings.HotfixLabel, HotUpdateSettings.AotLabel, HotUpdateSettings.ContentLabel })
                settings.AddLabel(label);
            settings.BuildRemoteCatalog = true;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
            GetOrCreateGroup(settings, HotGroup, true);
            GetOrCreateGroup(settings, AotGroup, false);
            GetOrCreateGroup(settings, ContentGroup, true);
            RegisterContent(settings, config);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HotUpdateSettings.BootstrapScenePath) == null)
                CreateBootstrap(config);
            // 业务场景通过 Addressables 加载，主包仅包含 AOT 启动场景。
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(HotUpdateSettings.BootstrapScenePath, true) };
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[HotUpdate] 两场景热更配置已准备。请确认 Addressables Profiles 的 Remote.LoadPath。");
        }

        public static void RegisterContent(AddressableAssetSettings settings, HotUpdateSettings config)
        {
            Register(settings, MainScenePath, ContentGroup, config.mainSceneAddress, HotUpdateSettings.ContentLabel);
            Register(settings, "Assets/ConfigData/Configs/ConfData.bytes", ContentGroup,
                ResEditorConfig.ConfsAddress, HotUpdateSettings.ContentLabel);
            foreach (string path in Directory.GetFiles(PrefabRoot, "*.prefab", SearchOption.AllDirectories))
            {
                string normalized = path.Replace('\\', '/');
                string address = normalized.Substring(PrefabRoot.Length + 1);
                Register(settings, normalized, ContentGroup, address.Substring(0, address.Length - ".prefab".Length),
                    HotUpdateSettings.ContentLabel);
            }
        }

        public static void Register(AddressableAssetSettings settings, string path, string group, string address, string label)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException($"资源不存在或尚未导入：{path}");
            var entry = settings.CreateOrMoveEntry(guid, settings.FindGroup(group));
            entry.SetAddress(address);
            entry.SetLabel(label, true, true);
            if (label == HotUpdateSettings.HotfixLabel) entry.SetLabel(HotUpdateSettings.AotLabel, false);
            if (label == HotUpdateSettings.AotLabel) entry.SetLabel(HotUpdateSettings.HotfixLabel, false);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true, true);
        }

        private static void GetOrCreateGroup(AddressableAssetSettings settings, string name, bool remote)
        {
            if (settings.FindGroup(name) != null) return;
            var group = settings.CreateGroup(name, false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var bundle = group.GetSchema<BundledAssetGroupSchema>();
            bundle.BuildPath.SetVariableByName(settings, remote ? AddressableAssetSettings.kRemoteBuildPath : AddressableAssetSettings.kLocalBuildPath);
            bundle.LoadPath.SetVariableByName(settings, remote ? AddressableAssetSettings.kRemoteLoadPath : AddressableAssetSettings.kLocalLoadPath);
            bundle.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            bundle.RetryCount = 2;
            bundle.Timeout = 30;
            // AOT 补充元数据固定于主包基线，补丁仅更新 HotUpdateDll 和 HotUpdateContent。
            group.GetSchema<ContentUpdateGroupSchema>().StaticContent = !remote;
        }

        private static void CreateBootstrap(HotUpdateSettings config)
        {
            Scene previous = SceneManager.GetActiveScene();
            bool untitled = string.IsNullOrEmpty(previous.path);
            if (untitled && previous.isDirty)
                throw new InvalidOperationException("请先保存当前未命名的场景，再创建热更启动场景。");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                untitled ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                var root = new GameObject("EntryManager");
                SceneManager.MoveGameObjectToScene(root, scene);
                var entry = root.AddComponent<EntryManager>();
                var serialized = new SerializedObject(entry);
                serialized.FindProperty("settings").objectReferenceValue = config;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene, HotUpdateSettings.BootstrapScenePath);
            }
            finally
            {
                if (!untitled)
                {
                    EditorSceneManager.CloseScene(scene, true);
                    if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                }
            }
        }

        private static void EnsureDirectory(string path) => Directory.CreateDirectory(path);
    }
}
