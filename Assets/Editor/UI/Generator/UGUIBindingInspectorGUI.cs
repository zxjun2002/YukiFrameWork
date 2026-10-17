using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class UGUIBindingInspectorGUI
{
    private static readonly Dictionary<string, Type> ComponentTypes = new Dictionary<string, Type>();

    internal static void DrawEntry(UGUIScriptGeneratorBindings settings, GameObject root, UGUIScriptGeneratorBindings.Entry entry)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(entry.componentType, EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        bool remove = GUILayout.Button("移除引用", EditorStyles.miniButton, GUILayout.Width(66));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("字段", entry.fieldName);

        if (root != null)
        {
            var target = UGUIBindingAccess.ResolveTarget(root, entry);
            EditorGUI.BeginChangeCheck();
            var replacement = (GameObject)EditorGUILayout.ObjectField("引用对象", target, typeof(GameObject), !EditorUtility.IsPersistent(root));
            if (EditorGUI.EndChangeCheck())
            {
                if (replacement != null && UGUIBindingAccess.ReferenceOf(replacement, entry.componentType) == null)
                    Debug.LogWarning($"该对象缺少组件 {entry.componentType}。");
                else
                {
                    try { UGUIBindingAccess.SetReference(root, settings, entry, replacement); }
                    catch (Exception exception) { Debug.LogWarning(exception.Message); }
                }
            }
        }

        var type = ResolveType(root, entry);
        var descriptors = UGUIScriptGeneratorEvents.GetEvents(type);
        foreach (var descriptor in descriptors)
        {
            var callback = entry.events.FirstOrDefault(e => e.eventName == descriptor.name);
            bool enabled = callback != null && callback.enabled;
            var label = new GUIContent(descriptor.Label, callback != null ? "回调：" + callback.methodName : "勾选后生成回调方法");
            bool newEnabled = EditorGUILayout.ToggleLeft(label, enabled);
            if (newEnabled == enabled) continue;
            Undo.RecordObject(settings, "修改 UI 监听");
            if (callback == null)
            {
                var naming = new UGUIScriptGeneratorContext { bindings = settings };
                var element = new UIElement(entry.fieldName, null) { bindingKey = entry.key };
                callback = naming.CreateEventBinding(element, descriptor.name);
                entry.events.Add(callback);
            }
            callback.enabled = newEnabled;
            UGUIBindingAccess.Save(settings);
        }
        foreach (var missing in entry.events.Where(e => e.enabled && descriptors.All(descriptor => descriptor.name != e.eventName)))
        {
            if (!EditorGUILayout.ToggleLeft(missing.eventName + "（事件不可用）", true))
            {
                Undo.RecordObject(settings, "取消失效 UI 监听");
                missing.enabled = false;
                UGUIBindingAccess.Save(settings);
            }
        }
        EditorGUILayout.EndVertical();
        if (remove)
        {
            Undo.RecordObject(settings, "移除 UI 引用");
            entry.active = false;
            UGUIBindingAccess.Save(settings);
        }
    }

    internal static void DrawAddReference(GameObject root, GameObject target)
    {
        if (root == null || target == null) return;
        string[] types = new UIElement("", target).GetAvailableComponentTypes();
        string preference = "UGUI_BINDING_ADD_TYPE_" + target.GetInstanceID();
        string selected = SessionState.GetString(preference, UGUIScriptGeneratorContext.GetDefaultComponentType(target));
        int index = Math.Max(0, Array.IndexOf(types, selected));
        EditorGUILayout.BeginHorizontal();
        int newIndex = EditorGUILayout.Popup(index, types);
        if (newIndex != index) SessionState.SetString(preference, types[newIndex]);
        bool add = GUILayout.Button("添加 UI 引用", GUILayout.Width(95));
        EditorGUILayout.EndHorizontal();
        if (!add) return;
        try
        {
            var context = new UGUIScriptGeneratorContext();
            context.LoadRoot(root);
            context.AddElement(target, types[newIndex]);
            context.SaveConfiguration();
        }
        catch (Exception exception) { Debug.LogError("[UGUI ScriptGen] " + exception.Message); }
    }

    internal static void DrawActions(GameObject root, UGUIScriptGeneratorBindings settings)
    {
        if (settings != null && settings.needsGeneration)
            EditorGUILayout.HelpBox("引用或监听已修改，更新生成代码后生效。", MessageType.Info);
        using (new EditorGUI.DisabledScope(root == null))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("更新生成代码"))
                EditorApplication.delayCall += () =>
                {
                    if (root == null) return;
                    var context = new UGUIScriptGeneratorContext();
                    context.LoadRoot(root);
                    context.GenerateScript();
                };
            if (GUILayout.Button("打开生成器")) UGUIScriptGenerator.OpenForRoot(root);
            EditorGUILayout.EndHorizontal();
        }
        if (settings != null && GUILayout.Button("查看全部 UI 引用", EditorStyles.miniButton))
        {
            EditorGUIUtility.PingObject(settings);
            Selection.activeObject = settings;
        }
    }

    private static Type ResolveType(GameObject root, UGUIScriptGeneratorBindings.Entry entry)
    {
        var reference = UGUIBindingAccess.ReferenceOf(UGUIBindingAccess.ResolveTarget(root, entry), entry.componentType);
        if (reference != null) return reference.GetType();
        if (entry.componentType == "GameObject") return typeof(GameObject);
        if (entry.componentType == "Transform") return typeof(Transform);
        if (ComponentTypes.TryGetValue(entry.componentType, out var cached)) return cached;
        var matches = TypeCache.GetTypesDerivedFrom<Component>().Where(type => type.Name == entry.componentType).Take(2).ToArray();
        var resolved = matches.Length == 1 ? matches[0] : null;
        ComponentTypes[entry.componentType] = resolved;
        return resolved;
    }
}
