using UnityEngine;

namespace Yuki.HotUpdate
{
    [CreateAssetMenu(menuName = "Yuki/Hot Update Settings")]
    public sealed class HotUpdateSettings : ScriptableObject
    {
        public const string AssetPath = "Assets/Main/HotUpdateSettings.asset";
        public const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        public const string ManifestAddress = "HotUpdate/Manifest";
        public const string HotfixLabel = "hotfix";
        public const string AotLabel = "aot";
        public const string ContentLabel = "content";

        public string mainSceneAddress = "Scenes/Main";
        public bool checkForCatalogUpdates = true;
        [Tooltip("编辑器复用 Unity 已编译的程序集，不重复 Assembly.Load。")]
        public string[] editorAssemblyNames = { "Config", "Runtime" };
    }

    [System.Serializable]
    public sealed class HotUpdateManifest
    {
        public string buildTarget;
        // 按依赖顺序生成，例如 Config.dll 在 Runtime.dll 之前。
        public string[] hotUpdateAssemblies;
        public string[] aotAssemblies;
    }
}
