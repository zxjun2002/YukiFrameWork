using System;
using System.Collections.Generic;
using MIKUFramework.IOC;
using UnityEngine.Events;

[Component]
public class EventCenter : IEventCenter
{
    // 事件字典：存储事件类型和对应的监听器
    private readonly Dictionary<Enum, UnityAction<BaseEventData>> _eventDic = new();

    /// <summary>
    /// 添加事件监听
    /// </summary>
    public void AddEventListener(Enum eventType, UnityAction<BaseEventData> action)
    {
        if (!_eventDic.ContainsKey(eventType))
        {
            _eventDic[eventType] = null;
        }

        var existingActions = _eventDic[eventType];
        if (existingActions != null && Array.Exists(existingActions.GetInvocationList(), d => d == (Delegate)action))
        {
            GameLogger.LogWarning($"重复的事件监听器：{eventType}");
            return;
        }

        _eventDic[eventType] += action;
    }

    /// <summary>
    /// 移除事件监听
    /// </summary>
    public void RemoveEventListener(Enum eventType, UnityAction<BaseEventData> action)
    {
        if (_eventDic.ContainsKey(eventType))
        {
            _eventDic[eventType] -= action;

            // 如果没有监听器，移除该事件类型
            if (_eventDic[eventType] == null)
            {
                _eventDic.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// 触发事件
    /// </summary>
    public void EventTrigger(BaseEventData eventData)
    {
        var eventType = eventData.EventType;

        if (_eventDic.TryGetValue(eventType, out var actions))
        {
            actions?.Invoke(eventData);
        }
        else
        {
            GameLogger.LogWarning($"未找到事件类型 {eventType} 的监听器！");
        }
    }

    /// <summary>
    /// 清空所有事件监听
    /// </summary>
    public void Clear()
    {
        _eventDic.Clear();
    }
}
