using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

internal sealed partial class UGUIScriptGeneratorContext
{
    internal static UGUIScriptGeneratorBindings FindBindings(string panelTypeName)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:UGUIScriptGeneratorBindings"))
        {
            var candidate = AssetDatabase.LoadAssetAtPath<UGUIScriptGeneratorBindings>(AssetDatabase.GUIDToAssetPath(guid));
            if (candidate != null && candidate.panelTypeName == panelTypeName) return candidate;
        }
        return null;
    }

    internal void RestoreBindings()
    {
        var panel = rootNode != null ? rootNode.GetComponent<BasePanel>() : null;
        string fullName = panel != null ? panel.GetType().FullName :
            string.IsNullOrEmpty(NameSpaceName) ? rootNode?.name : $"{NameSpaceName}.{rootNode?.name}";
        if (bindings == null || bindings.panelTypeName != fullName) bindings = FindBindings(fullName);
        foreach (var element in uiElements)
        {
            if (element.uiObject == null) continue;
            if (element.eventsInitialized)
            {
                // 配置中的方法名是权威映射；域重载后窗口可能还保留修改前的名称。
                var savedEntry = bindings?.entries.FirstOrDefault(candidate => candidate.key == element.bindingKey);
                if (savedEntry != null)
                    foreach (var callback in element.events)
                    {
                        var savedEvent = savedEntry.events.FirstOrDefault(candidate => candidate.eventName == callback.eventName);
                        if (savedEvent != null) callback.methodName = savedEvent.methodName;
                    }
                continue;
            }
            string sourceId = GlobalObjectId.GetGlobalObjectIdSlow(element.uiObject).ToString();
            var entry = bindings?.entries.OrderByDescending(candidate => candidate.active).FirstOrDefault(candidate =>
                candidate.componentType == element.selectedComponentType &&
                ((element.isLocked && candidate.fieldName == element.fieldName) ||
                 (candidate.sourceGlobalId == sourceId && HasPersistentId(element.uiObject)) ||
                 IsBoundToElement(panel, candidate.fieldName, element)));
            if (entry != null)
            {
                element.bindingKey = entry.key;
                element.fieldName = entry.fieldName;
                element.events = entry.events.Select(e => e.Copy()).ToList();
                element.eventsInitialized = true;
                element.isLocked = true;
            }
            InitializeEvents(element);
        }
    }

    internal static bool HasPersistentId(GameObject target)
    {
        return EditorUtility.IsPersistent(target) || !string.IsNullOrEmpty(target.scene.path);
    }

    internal static bool IsBoundToElement(BasePanel panel, string fieldName, UIElement element)
    {
        var field = panel?.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        var value = field?.GetValue(panel);
        return value is GameObject go ? go == element.uiObject :
            value is Component component && component.gameObject == element.uiObject && component.GetType().Name == element.selectedComponentType;
    }

    internal void SaveBindings(string classFolderPath, string fullClassName)
    {
        if (bindings == null)
        {
            string editorPath = Path.Combine(classFolderPath, "Editor").Replace('\\', '/');
            Directory.CreateDirectory(editorPath);
            AssetDatabase.Refresh();
            bindings = ScriptableObject.CreateInstance<UGUIScriptGeneratorBindings>();
            AssetDatabase.CreateAsset(bindings, $"{editorPath}/{Path.GetFileName(classFolderPath)}.UIBindings.asset");
        }
        bindings.panelTypeName = fullClassName;
        Undo.RecordObject(bindings, "保存 UI 引用配置");
        bindings.version = 1;
        UGUIBindingAccess.CaptureRoot(bindings, rootNode);
        foreach (var entry in bindings.entries) entry.active = false;
        foreach (var element in uiElements.Where(e => e != null && (e.uiObject != null || (e.isLocked && !string.IsNullOrEmpty(e.bindingKey)))))
        {
            var entry = bindings.entries.FirstOrDefault(candidate => candidate.key == element.bindingKey);
            if (entry == null)
            {
                entry = new UGUIScriptGeneratorBindings.Entry { key = element.bindingKey };
                bindings.entries.Add(entry);
            }
            entry.fieldName = element.fieldName;
            entry.componentType = element.selectedComponentType;
            entry.active = true;
            UGUIBindingAccess.CaptureTarget(entry, element.uiObject);
            var panel = rootNode.GetComponent<BasePanel>();
            var field = panel?.GetType().GetField(element.fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            entry.pendingReference = field == null || UGUIBindingAccess.GameObjectOf(field.GetValue(panel) as UnityEngine.Object) != element.uiObject;
            entry.events = element.events.Select(e => e.Copy()).ToList();
        }
        UGUIBindingAccess.Save(bindings);
    }
}
