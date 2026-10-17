using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class UGUIBindingInspector
{
    static UGUIBindingInspector()
    {
        Editor.finishedDefaultHeaderGUI += DrawHeader;
        EditorApplication.projectChanged += UGUIBindingAccess.ClearCache;
        Undo.undoRedoPerformed += OnUndoRedo;
        EditorSceneManager.sceneSaved += OnSceneSaved;
    }

    private static void OnSceneSaved(Scene scene)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:UGUIScriptGeneratorBindings"))
        {
            var settings = AssetDatabase.LoadAssetAtPath<UGUIScriptGeneratorBindings>(AssetDatabase.GUIDToAssetPath(guid));
            if (settings == null) continue;
            var root = UGUIBindingAccess.ResolveRoot(settings);
            if (root == null || root.scene != scene) continue;
            UGUIBindingAccess.CaptureRoot(settings, root);
            foreach (var entry in settings.entries.Where(entry => entry.active))
            {
                var target = UGUIBindingAccess.ResolveTarget(root, entry);
                if (target != null) UGUIBindingAccess.CaptureTarget(entry, target);
            }
            UGUIBindingAccess.Save(settings, false);
        }
    }

    private static void OnUndoRedo()
    {
        UGUIBindingAccess.ClearCache();
        var root = UGUIBindingAccess.FindRoot(Selection.activeGameObject);
        var settings = Selection.activeObject as UGUIScriptGeneratorBindings ?? UGUIBindingAccess.SettingsFor(root);
        if (settings != null) UGUIBindingAccess.Notify(settings);
    }

    private static void DrawHeader(Editor editor)
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (editor.targets.Length != 1 || !(editor.target is GameObject selected) || !selected.scene.IsValid()) return;
        var root = UGUIBindingAccess.FindRoot(selected);
        if (root == null) return;
        var settings = UGUIBindingAccess.SettingsFor(root);
        UGUIBindingAccess.Upgrade(settings, root);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("UI 引用", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("所属面板", root, typeof(GameObject), true);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
        {
            if (settings != null)
            {
                foreach (var entry in settings.entries.Where(entry => entry.active && UGUIBindingAccess.ResolveTarget(root, entry) == selected).ToArray())
                    UGUIBindingInspectorGUI.DrawEntry(settings, root, entry);
                if (selected == root)
                    EditorGUILayout.LabelField("已配置引用", settings.entries.Count(entry => entry.active).ToString());
            }
            UGUIBindingInspectorGUI.DrawAddReference(root, selected);
            UGUIBindingInspectorGUI.DrawActions(root, settings);
        }
        EditorGUILayout.EndVertical();
    }
}

[CustomEditor(typeof(UGUIScriptGeneratorBindings))]
internal sealed class UGUIBindingsAssetEditor : Editor
{
    private GameObject addTarget;

    public override void OnInspectorGUI()
    {
        var settings = (UGUIScriptGeneratorBindings)target;
        var root = UGUIBindingAccess.ResolveRoot(settings);
        UGUIBindingAccess.Upgrade(settings, root);
        EditorGUILayout.LabelField("UI 引用配置", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("面板类型", settings.panelTypeName);
        var selectedRoot = (GameObject)EditorGUILayout.ObjectField("面板对象", root, typeof(GameObject), true);
        if (selectedRoot != root && selectedRoot != null)
        {
            if (UGUIBindingAccess.PanelTypeName(selectedRoot) == settings.panelTypeName)
            {
                Undo.RecordObject(settings, "选择 UI 面板对象");
                UGUIBindingAccess.CaptureRoot(settings, selectedRoot);
                UGUIBindingAccess.Save(settings, false);
                root = selectedRoot;
            }
            else Debug.LogWarning("请选择配置对应类型的面板对象。");
        }
        if (root == null) EditorGUILayout.HelpBox("请在场景或 Prefab 编辑模式中打开对应面板，以修改对象引用和生成代码。", MessageType.Info);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
        {
            foreach (var entry in settings.entries.Where(entry => entry.active).ToArray())
                UGUIBindingInspectorGUI.DrawEntry(settings, root, entry);
            if (root != null)
            {
                addTarget = (GameObject)EditorGUILayout.ObjectField("新增对象", addTarget, typeof(GameObject), true);
                if (addTarget != null && UGUIBindingAccess.FindRoot(addTarget) == root)
                    UGUIBindingInspectorGUI.DrawAddReference(root, addTarget);
                else if (addTarget != null) EditorGUILayout.HelpBox("新增对象必须属于当前面板。", MessageType.Warning);
            }
            UGUIBindingInspectorGUI.DrawActions(root, settings);
        }
    }
}
