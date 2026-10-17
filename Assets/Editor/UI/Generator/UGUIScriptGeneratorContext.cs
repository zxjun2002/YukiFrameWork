using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
internal sealed partial class UGUIScriptGeneratorContext
{
    public List<UIElement> uiElements = new List<UIElement>();
    public string scriptOutputPath = string.Empty;
    public GameObject rootNode;
    public bool isRootNodeChecked;
    public UGUIScriptGeneratorBindings bindings;
    public string NameSpaceName = "Yuki";

    internal static readonly List<Type> componentPriority = new List<Type>
    {
        // TODO: 自定义类型
        typeof(UIList),
        typeof(UIListMulti),
        typeof(EnhancedScrollerController),
        typeof(Button),
        typeof(Toggle),
        typeof(TMP_InputField),
        typeof(InputField),
        typeof(TMP_Dropdown),
        typeof(Dropdown),
        typeof(Text),
        typeof(Slider),
        typeof(Scrollbar),
        typeof(ScrollRect),
        typeof(Image),
        typeof(TextMeshProUGUI)
    };

    internal void AutoUpdateFieldNameForNew(UIElement e)
    {
        if (e == null || e.uiObject == null) return;
        if (e.isLocked) return;

        if (string.IsNullOrEmpty(e.selectedComponentType))
            e.selectedComponentType = GetDefaultComponentType(e.uiObject);

        e.fieldName = $"{SanitizeIdentifier(e.uiObject.name)}_{e.selectedComponentType}";
    }

    internal UGUIEventBinding CreateEventBinding(UIElement element, string eventName)
    {
        // GUID 仅作为配置中的身份，不放进业务方法名；历史配置也参与重名检查。
        var usedNames = new HashSet<string>(uiElements.Where(e => e != null).SelectMany(e => e.events).Select(e => e.methodName));
        if (bindings != null)
            usedNames.UnionWith(bindings.entries.SelectMany(entry => entry.events).Select(e => e.methodName));
        string baseName = $"{element.fieldName}_{SanitizeIdentifier(eventName)}";
        string methodName = baseName;
        for (int suffix = 2; usedNames.Contains(methodName); suffix++) methodName = baseName + "_" + suffix;
        return new UGUIEventBinding
        {
            eventName = eventName,
            methodName = methodName
        };
    }

    internal void InitializeEvents(UIElement element)
    {
        if (element.eventsInitialized) return;
        if (string.IsNullOrEmpty(element.bindingKey)) element.bindingKey = Guid.NewGuid().ToString("N");
        var available = UGUIScriptGeneratorEvents.GetEvents(GetSelectedComponentTypeFromObject(element));
        string defaultEvent = IsButtonLike(element) ? "onClick" : "onValueChanged";
        if (available.Any(descriptor => descriptor.name == defaultEvent))
        {
            var callback = CreateEventBinding(element, defaultEvent);
            // 老生成器只支持 Button/Toggle，保持这些控件原有的方法名。
            if (element.isLocked && (IsButtonLike(element) || IsToggleLike(element)))
                callback.methodName = element.fieldName + "Callback";
            element.events.Add(callback);
        }
        element.eventsInitialized = true;
    }

    internal static string SanitizeIdentifier(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "Field";
        raw = Regex.Replace(raw, @"\s+", "");
        raw = Regex.Replace(raw, @"[^\w]", "_");
        if (char.IsDigit(raw[0])) raw = "_" + raw;
        return raw;
    }

    internal static string GetDefaultComponentType(GameObject go)
    {
        if (go == null) return "GameObject";

        var components = go.GetComponents<Component>();
        foreach (var type in componentPriority)
        {
            foreach (var component in components)
            {
                if (component == null) continue;
                var ct = component.GetType();
                if (type.IsAssignableFrom(ct))
                    return ct.Name;
            }
        }
        return "GameObject";
    }

    // 用“物体上真实组件类型”判断 Button/Toggle 派生（避免字符串解析歧义）
    internal static Type GetSelectedComponentTypeFromObject(UIElement e)
    {
        if (e == null || e.uiObject == null || string.IsNullOrEmpty(e.selectedComponentType))
            return null;

        foreach (var c in e.uiObject.GetComponents<Component>())
        {
            if (c == null) continue;
            if (c.GetType().Name == e.selectedComponentType)
                return c.GetType();
        }

        // Transform 也是 Component，但不会出现在 GetComponents<Component>() 里？会的
        if (e.selectedComponentType == "Transform") return typeof(Transform);
        if (e.selectedComponentType == "GameObject") return typeof(GameObject);

        return null;
    }

    internal static bool IsButtonLike(UIElement e)
    {
        var t = GetSelectedComponentTypeFromObject(e);
        return t != null && typeof(Button).IsAssignableFrom(t);
    }

    internal static bool IsToggleLike(UIElement e)
    {
        var t = GetSelectedComponentTypeFromObject(e);
        return t != null && typeof(Toggle).IsAssignableFrom(t);
    }
}
