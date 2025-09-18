using System;
using System.Collections.Generic;
using MIKUFramework.IOC;

[Component]
public class EventCenter : IEventCenter
{
    // 使用 Delegate 存储无参和有参事件
    private readonly Dictionary<Enum, Delegate> _eventDic = new();

    /// <summary>
    /// 添加无参事件监听
    /// </summary>
    public void AddEventListener(Enum eventType, Action action)
    {
        if (_eventDic.TryGetValue(eventType, out var existingDelegate))
        {
            if (existingDelegate is Action existingAction)
            {
                if (Array.Exists(existingAction.GetInvocationList(), d => d == (Delegate)action))
                {
                    GameLogger.LogWarning($"重复的无参事件监听器：{eventType}");
                    return;
                }

                _eventDic[eventType] = existingAction + action;
            }
            else
            {
                throw new InvalidOperationException($"事件类型 {eventType} 的监听器类型不匹配。");
            }
        }
        else
        {
            _eventDic[eventType] = action;
        }
    }

    /// <summary>
    /// 添加有参事件监听
    /// </summary>
    public void AddEventListener(Enum eventType, Action<BaseEventData> action)
    {
        if (_eventDic.TryGetValue(eventType, out var existingDelegate))
        {
            if (existingDelegate is Action<BaseEventData> existingAction)
            {
                if (Array.Exists(existingAction.GetInvocationList(), d => d == (Delegate)action))
                {
                    GameLogger.LogWarning($"重复的有参事件监听器：{eventType}");
                    return;
                }

                _eventDic[eventType] = existingAction + action;
            }
            else
            {
                throw new InvalidOperationException($"事件类型 {eventType} 的监听器类型不匹配。");
            }
        }
        else
        {
            _eventDic[eventType] = action;
        }
    }

    /// <summary>
    /// 移除无参事件监听
    /// </summary>
    public void RemoveEventListener(Enum eventType, Action action)
    {
        if (_eventDic.TryGetValue(eventType, out var existingDelegate) && existingDelegate is Action existingAction)
        {
            var newAction = existingAction - action;

            if (newAction == null)
            {
                _eventDic.Remove(eventType);
            }
            else
            {
                _eventDic[eventType] = newAction;
            }
        }
    }

    /// <summary>
    /// 移除有参事件监听
    /// </summary>
    public void RemoveEventListener(Enum eventType, Action<BaseEventData> action)
    {
        if (_eventDic.TryGetValue(eventType, out var existingDelegate) && existingDelegate is Action<BaseEventData> existingAction)
        {
            var newAction = existingAction - action;

            if (newAction == null)
            {
                _eventDic.Remove(eventType);
            }
            else
            {
                _eventDic[eventType] = newAction;
            }
        }
    }

    /// <summary>
    /// 触发无参事件
    /// </summary>
    public void EventTrigger(Enum eventType)
    {
        if (_eventDic.TryGetValue(eventType, out var existingDelegate) && existingDelegate is Action action)
        {
            action.Invoke();
        }
        else
        {
            GameLogger.LogWarning($"未找到事件类型 {eventType} 的无参监听器！");
        }
    }

    /// <summary>
    /// 触发有参事件
    /// </summary>
    public void EventTrigger(BaseEventData eventData)
    {
        if (_eventDic.TryGetValue(eventData.EventType, out var existingDelegate) && existingDelegate is Action<BaseEventData> action)
        {
            action.Invoke(eventData);
        }
        else
        {
            GameLogger.LogWarning($"未找到事件类型 {eventData.EventType} 的有参监听器！");
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
