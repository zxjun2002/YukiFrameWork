using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Yuki.HotUpdate;

namespace Yuki.Editor.HotUpdate
{
    public sealed class HotUpdateBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (HotUpdateBuild.GeneratingAot || EditorUserBuildSettings.buildScriptsOnly) return;
            HotUpdateBuild.ValidateBackend();
            HotUpdateBuild.ValidateContent();
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            if (scenes.Length != 1 || scenes[0].path != HotUpdateSettings.BootstrapScenePath)
                throw new BuildFailedException("主包仅应包含 Bootstrap 场景，业务场景通过 Addressables 加载。");
        }
    }

    // 与已验证项目一致，兼容较新 MSVC 对旧版 Unity IL2CPP 的 stdext/hash 警告。
    public sealed class MsvcStdextWorkaround : IPreprocessBuildWithReport
    {
        public int callbackOrder => -200;
        public void OnPreprocessBuild(BuildReport report)
        {
            const string flag = "/D_SILENCE_STDEXT_HASH_DEPRECATION_WARNINGS";
            string current = Environment.GetEnvironmentVariable("_CL_") ?? "";
            if (!current.Contains(flag)) Environment.SetEnvironmentVariable("_CL_", (current + " " + flag).Trim());
        }
    }
}
