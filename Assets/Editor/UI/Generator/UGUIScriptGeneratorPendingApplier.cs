using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[InitializeOnLoad]
internal static class UGUIScriptGeneratorPendingApplier
{
    private const string KEY = "UGUI_SCRIPTGEN_PENDING_JSON";

    [Serializable]
    private class PendingItem
    {
        public string fieldName;
        public string targetGid; // GameObject GlobalObjectId
        public int targetInstanceId;
    }

    [Serializable]
    private class PendingPayload
    {
        public string rootGid;        // root GameObject GlobalObjectId
        public int rootInstanceId;
        public string fullClassName;  // e.g. Yuki.AUIPanel
        public List<PendingItem> items = new List<PendingItem>();
        public int retry;
    }

    static UGUIScriptGeneratorPendingApplier()
    {
        // 域重载后会执行这里，确保能把生成的脚本挂回去
        EditorApplication.delayCall += TryApplyPending;
        CompilationPipeline.compilationFinished += _ => EditorApplication.delayCall += TryApplyPending;
    }

    public static void SetPending(GameObject root, string fullClassName, List<UIElement> elements)
    {
        if (root == null) return;

        var payload = new PendingPayload
        {
            rootGid = GlobalObjectId.GetGlobalObjectIdSlow(root).ToString(),
            rootInstanceId = root.GetInstanceID(),
            fullClassName = fullClassName,
            retry = 0
        };

        foreach (var e in elements)
        {
            if (e == null || e.uiObject == null) continue;

            payload.items.Add(new PendingItem
            {
                fieldName = e.fieldName,
                targetGid = GlobalObjectId.GetGlobalObjectIdSlow(e.uiObject).ToString(),
                targetInstanceId = e.uiObject.GetInstanceID()
            });
        }

        SessionState.SetString(KEY, JsonUtility.ToJson(payload));
        EditorApplication.delayCall += TryApplyPending;
    }

    private static void TryApplyPending()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { Reschedule(); return; }

        string json = SessionState.GetString(KEY, "");
        if (string.IsNullOrEmpty(json)) return;

        var payload = JsonUtility.FromJson<PendingPayload>(json);
        if (payload == null) { SessionState.EraseString(KEY); return; }

        var rootObj = (ObjectFromGid(payload.rootGid) ?? EditorUtility.InstanceIDToObject(payload.rootInstanceId)) as GameObject;
        if (rootObj == null)
        {
            Debug.LogWarning("[UGUI ScriptGen] Pending apply failed: root not found.");
            SessionState.EraseString(KEY);
            return;
        }

        var scriptType = FindType(payload.fullClassName);
        if (scriptType == null || !typeof(Component).IsAssignableFrom(scriptType))
        {
            if (payload.retry++ < 50)
            {
                SessionState.SetString(KEY, JsonUtility.ToJson(payload));
                Reschedule();
                return;
            }

            Debug.LogWarning($"[UGUI ScriptGen] Pending apply failed: type not found or not Component: {payload.fullClassName}");
            SessionState.EraseString(KEY);
            return;
        }

        try
        {
            var comp = rootObj.GetComponent(scriptType) ?? rootObj.AddComponent(scriptType);

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            Undo.RecordObject(comp, "Auto Assign UI Elements");
            EditorUtility.SetDirty(comp);

            foreach (var item in payload.items)
            {
                var field = scriptType.GetField(item.fieldName, flags);
                if (field == null) continue;

                var targetGo = (ObjectFromGid(item.targetGid) ?? EditorUtility.InstanceIDToObject(item.targetInstanceId)) as GameObject;
                if (targetGo == null) continue;

                var ft = field.FieldType;

                object value = null;
                if (ft == typeof(GameObject)) value = targetGo;
                else if (ft == typeof(Transform)) value = targetGo.transform;
                else if (typeof(Component).IsAssignableFrom(ft))
                    value = targetGo.GetComponent(ft);

                if (value != null)
                    field.SetValue(comp, value);
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(comp);

            if (PrefabUtility.IsPartOfPrefabInstance(rootObj))
                PrefabUtility.ApplyPrefabInstance(rootObj, InteractionMode.AutomatedAction);

            UGUIBindingAccess.OnReferencesApplied(rootObj);

            Debug.Log($"[UGUI ScriptGen] Auto-attached & assigned: {payload.fullClassName} -> {rootObj.name}");
            SessionState.EraseString(KEY);
        }
        catch (Exception ex)
        {
            Debug.LogError("[UGUI ScriptGen] Pending apply exception:\n" + ex);
            SessionState.EraseString(KEY);
        }
    }

    private static void Reschedule()
    {
        EditorApplication.delayCall += TryApplyPending;
    }

    private static UnityEngine.Object ObjectFromGid(string gidStr)
    {
        if (string.IsNullOrEmpty(gidStr)) return null;
        if (!GlobalObjectId.TryParse(gidStr, out var gid)) return null;
        return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
    }

    private static Type FindType(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(fullName);
            if (t != null) return t;
        }
        return null;
    }
}
