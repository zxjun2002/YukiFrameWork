using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

internal sealed partial class UGUIScriptGeneratorContext
{
    internal void LoadRoot(GameObject root)
    {
        rootNode = root;
        uiElements.Clear();
        if (root == null) { isRootNodeChecked = false; return; }

        var panel = root.GetComponent<BasePanel>();
        NameSpaceName = panel != null ? panel.GetType().Namespace ?? "" : "Yuki";
        string className = panel != null ? panel.GetType().Name : root.name;
        string fullName = string.IsNullOrEmpty(NameSpaceName) ? className : NameSpaceName + "." + className;
        bindings = FindBindings(fullName);
        UGUIBindingAccess.Upgrade(bindings, root);

        scriptOutputPath = EditorPrefs.GetString("ScriptOutputPath", "Assets/Scripts/UI/");
        if (bindings != null)
        {
            string settingsPath = AssetDatabase.GetAssetPath(bindings);
            string classFolder = Path.GetDirectoryName(Path.GetDirectoryName(settingsPath));
            if (!string.IsNullOrEmpty(classFolder)) scriptOutputPath = Path.GetDirectoryName(classFolder);
        }
        if (panel != null)
        {
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(panel));
            if (!string.IsNullOrEmpty(scriptPath))
                scriptOutputPath = Path.GetDirectoryName(Path.GetDirectoryName(scriptPath));

            foreach (var field in panel.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (!field.IsPrivate || !field.IsDefined(typeof(SerializeField), true)) continue;
                if (!typeof(Component).IsAssignableFrom(field.FieldType) && field.FieldType != typeof(GameObject)) continue;
                var entry = bindings?.entries.FirstOrDefault(candidate => candidate.active && candidate.fieldName == field.Name);
                if (entry == null && bindings != null && bindings.entries.Any(candidate => candidate.fieldName == field.Name)) continue;
                var value = field.GetValue(panel) as UnityEngine.Object;
                var target = entry != null ? UGUIBindingAccess.ResolveTarget(root, entry) : UGUIBindingAccess.GameObjectOf(value);
                if (target == null && entry == null) continue;
                uiElements.Add(new UIElement(field.Name, target)
                {
                    selectedComponentType = field.FieldType.Name,
                    isLocked = true,
                    bindingKey = entry?.key,
                    events = entry != null ? entry.events.Select(e => e.Copy()).ToList() : new System.Collections.Generic.List<UGUIEventBinding>(),
                    eventsInitialized = entry != null
                });
            }
        }

        if (bindings != null)
        {
            foreach (var entry in bindings.entries.Where(candidate => candidate.active))
            {
                if (uiElements.Any(element => element.fieldName == entry.fieldName)) continue;
                uiElements.Add(new UIElement(entry.fieldName, UGUIBindingAccess.ResolveTarget(root, entry))
                {
                    selectedComponentType = entry.componentType,
                    bindingKey = entry.key,
                    events = entry.events.Select(e => e.Copy()).ToList(),
                    eventsInitialized = true,
                    isLocked = true
                });
            }
        }
        RestoreBindings();
        isRootNodeChecked = true;
    }

    internal UIElement AddElement(GameObject target, string componentType = null)
    {
        componentType = componentType ?? GetDefaultComponentType(target);
        var existing = uiElements.FirstOrDefault(element => element.uiObject == target && element.selectedComponentType == componentType);
        if (existing != null) return existing;
        var elementToAdd = new UIElement("", target) { selectedComponentType = componentType };
        AutoUpdateFieldNameForNew(elementToAdd);
        uiElements.Add(elementToAdd);
        RestoreBindings();
        return elementToAdd;
    }

    internal void SaveConfiguration()
    {
        if (rootNode == null) throw new InvalidOperationException("没有关联的 UI 面板。");
        foreach (var element in uiElements)
        {
            AutoUpdateFieldNameForNew(element);
            InitializeEvents(element);
        }
        var conflict = ValidateConflicts();
        if (conflict != null) throw new InvalidOperationException(conflict.message);
        var panel = rootNode.GetComponent<BasePanel>();
        string className = panel != null ? panel.GetType().Name : rootNode.name;
        SaveBindings(Path.Combine(scriptOutputPath, className),
            string.IsNullOrEmpty(NameSpaceName) ? className : NameSpaceName + "." + className);
    }
}
