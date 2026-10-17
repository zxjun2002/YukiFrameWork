using System;
using System.Collections.Generic;
using UnityEngine;

// 只供编辑器生成代码使用；不参与运行时事件分发。
public sealed class UGUIScriptGeneratorBindings : ScriptableObject
{
    public string panelTypeName;
    public int version;
    public string rootGlobalId;
    public int rootInstanceId;
    public string rootSessionId;
    public bool needsGeneration;
    public List<Entry> entries = new List<Entry>();

    [Serializable]
    public sealed class Entry
    {
        public string key;
        public string fieldName;
        public string componentType;
        public string sourceGlobalId;
        public int sourceInstanceId;
        public string sourceSessionId;
        public bool active = true;
        public bool pendingReference;
        public List<UGUIEventBinding> events = new List<UGUIEventBinding>();
    }
}

[Serializable]
public sealed class UGUIEventBinding
{
    public string eventName;
    public string methodName;
    public bool enabled = true;

    public UGUIEventBinding Copy() => new UGUIEventBinding { eventName = eventName, methodName = methodName, enabled = enabled };
}
