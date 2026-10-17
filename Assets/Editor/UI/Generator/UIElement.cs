using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class UIElement
{
    public string fieldName;
    public GameObject uiObject;
    public string selectedComponentType;
    public bool isLocked;
    public string bindingKey;
    public bool eventsInitialized;
    public List<UGUIEventBinding> events = new List<UGUIEventBinding>();

    public UIElement(string fieldName, GameObject uiObject)
    {
        this.fieldName = fieldName;
        this.uiObject = uiObject;
        selectedComponentType = string.Empty;
        isLocked = false;
    }

    public string[] GetAvailableComponentTypes()
    {
        if (uiObject == null)
            return Array.Empty<string>();

        return uiObject.GetComponents<Component>()
            .Where(c => c != null)
            .Select(c => c.GetType().Name)
            .Prepend("GameObject")
            .Prepend("Transform")
            .Distinct()
            .ToArray();
    }

    public string GetComponentType() => selectedComponentType;

    public void UpdateSelectedComponentType(string newType)
    {
        selectedComponentType = newType;
        events.Clear();
        eventsInitialized = false;
        bindingKey = null;
        // 字段名由生成器统一控制（新控件自动 name+type，老控件锁定不改）
    }
}
