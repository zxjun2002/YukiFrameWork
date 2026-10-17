using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

internal static class UGUIBindingAccess
{
    internal static event Action<UGUIScriptGeneratorBindings> Changed;
    private static readonly Dictionary<string, UGUIScriptGeneratorBindings> SettingsCache = new Dictionary<string, UGUIScriptGeneratorBindings>();
    private static bool notifyQueued;
    private static readonly HashSet<UGUIScriptGeneratorBindings> PendingChanges = new HashSet<UGUIScriptGeneratorBindings>();
    private const string SessionKey = "UGUI_BINDING_EDITOR_SESSION";

    private static string SessionId
    {
        get
        {
            string value = SessionState.GetString(SessionKey, "");
            if (!string.IsNullOrEmpty(value)) return value;
            value = Guid.NewGuid().ToString("N");
            SessionState.SetString(SessionKey, value);
            return value;
        }
    }

    internal static GameObject FindRoot(GameObject target)
    {
        for (var current = target != null ? target.transform : null; current != null; current = current.parent)
            if (current.GetComponent<BasePanel>() != null || current.name.EndsWith("UIPanel", StringComparison.Ordinal))
                return current.gameObject;
        return null;
    }

    internal static string PanelTypeName(GameObject root)
    {
        var panel = root != null ? root.GetComponent<BasePanel>() : null;
        return panel != null ? panel.GetType().FullName : root != null ? "Yuki." + root.name : "";
    }

    internal static UGUIScriptGeneratorBindings SettingsFor(GameObject root)
    {
        if (root == null) return null;
        string name = PanelTypeName(root);
        if (SettingsCache.TryGetValue(name, out var cached)) return cached;
        var settings = UGUIScriptGeneratorContext.FindBindings(name);
        SettingsCache[name] = settings;
        return settings;
    }

    internal static void ClearCache() => SettingsCache.Clear();

    internal static GameObject GameObjectOf(UnityEngine.Object value) => value is GameObject go ? go : (value as Component)?.gameObject;

    internal static GameObject ResolveTarget(GameObject root, UGUIScriptGeneratorBindings.Entry entry)
    {
        if (root == null) return null;
        var panel = root.GetComponent<BasePanel>();
        var field = panel?.GetType().GetField(entry.fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (!entry.pendingReference && field != null) return GameObjectOf(field.GetValue(panel) as UnityEngine.Object);
        var resolved = ObjectFromId(entry.sourceGlobalId) as GameObject;
        if (resolved != null && FindRoot(resolved) == root) return resolved;
        var fallback = EditorUtility.InstanceIDToObject(entry.sourceInstanceId) as GameObject;
        if (fallback != null && FindRoot(fallback) == root && entry.sourceSessionId == SessionId) return fallback;
        return null;
    }

    internal static GameObject ResolveRoot(UGUIScriptGeneratorBindings settings)
    {
        var resolved = ObjectFromId(settings.rootGlobalId) as GameObject;
        if (resolved != null && PanelTypeName(resolved) == settings.panelTypeName) return resolved;
        var fallback = EditorUtility.InstanceIDToObject(settings.rootInstanceId) as GameObject;
        if (fallback != null && PanelTypeName(fallback) == settings.panelTypeName && settings.rootSessionId == SessionId) return fallback;
        var candidates = Resources.FindObjectsOfTypeAll<BasePanel>()
            .Where(panel => panel.gameObject.scene.IsValid() && panel.GetType().FullName == settings.panelTypeName)
            .Select(panel => panel.gameObject).Take(2).ToArray();
        return candidates.Length == 1 ? candidates[0] : null;
    }

    internal static UnityEngine.Object ReferenceOf(GameObject target, string componentType)
    {
        if (target == null) return null;
        if (componentType == "GameObject") return target;
        if (componentType == "Transform") return target.transform;
        return target.GetComponents<Component>().FirstOrDefault(component => component != null && component.GetType().Name == componentType);
    }

    internal static void CaptureTarget(UGUIScriptGeneratorBindings.Entry entry, GameObject target)
    {
        entry.sourceGlobalId = target != null ? GlobalObjectId.GetGlobalObjectIdSlow(target).ToString() : "";
        entry.sourceInstanceId = target != null ? target.GetInstanceID() : 0;
        entry.sourceSessionId = SessionId;
    }

    internal static void CaptureRoot(UGUIScriptGeneratorBindings settings, GameObject root)
    {
        settings.rootGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(root).ToString();
        settings.rootInstanceId = root.GetInstanceID();
        settings.rootSessionId = SessionId;
    }

    internal static void SetReference(GameObject root, UGUIScriptGeneratorBindings settings, UGUIScriptGeneratorBindings.Entry entry, GameObject target)
    {
        if (target != null && FindRoot(target) != root) throw new InvalidOperationException("引用必须属于同一个 UI 面板。");
        Undo.RecordObject(settings, "修改 UI 引用");
        CaptureTarget(entry, target);
        entry.pendingReference = true;
        var panel = root.GetComponent<BasePanel>();
        if (panel != null)
        {
            var serialized = new SerializedObject(panel);
            var property = serialized.FindProperty(entry.fieldName);
            if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue = ReferenceOf(target, entry.componentType);
                serialized.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(panel);
                entry.pendingReference = false;
            }
        }
        Save(settings, entry.pendingReference);
    }

    internal static void Upgrade(UGUIScriptGeneratorBindings settings, GameObject root)
    {
        if (settings == null || root == null || settings.version > 0) return;
        Undo.RecordObject(settings, "升级 UI 引用配置");
        var panel = root.GetComponent<BasePanel>();
        foreach (var entry in settings.entries)
            entry.active = panel == null || panel.GetType().GetField(entry.fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null;
        settings.version = 1;
        CaptureRoot(settings, root);
        Save(settings, false);
    }

    internal static void OnReferencesApplied(GameObject root)
    {
        var settings = SettingsFor(root);
        if (settings == null) return;
        CaptureRoot(settings, root);
        foreach (var entry in settings.entries.Where(candidate => candidate.active))
        {
            entry.pendingReference = false;
            CaptureTarget(entry, ResolveTarget(root, entry));
        }
        Save(settings, false);
    }

    internal static void Save(UGUIScriptGeneratorBindings settings, bool needsGeneration = true)
    {
        if (needsGeneration) settings.needsGeneration = true;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssetIfDirty(settings);
        Notify(settings);
    }

    internal static void Notify(UGUIScriptGeneratorBindings settings)
    {
        SettingsCache.Remove(settings.panelTypeName);
        PendingChanges.Add(settings);
        if (notifyQueued) return;
        notifyQueued = true;
        EditorApplication.delayCall += DispatchChanges;
    }

    private static void DispatchChanges()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += DispatchChanges;
            return;
        }
        notifyQueued = false;
        var changes = PendingChanges.ToArray();
        PendingChanges.Clear();
        foreach (var changed in changes) if (changed != null) Changed?.Invoke(changed);
        if (!Application.isBatchMode)
        {
            EditorApplication.RepaintHierarchyWindow();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }

    private static UnityEngine.Object ObjectFromId(string id) =>
        !string.IsNullOrEmpty(id) && GlobalObjectId.TryParse(id, out var value) ? GlobalObjectId.GlobalObjectIdentifierToObjectSlow(value) : null;
}
