using System;
using UnityEngine.Events;

public interface IEventCenter
{
    void AddEventListener(Enum eventType, UnityAction action);
    void AddEventListener(Enum eventType, UnityAction<BaseEventData> action);
    void RemoveEventListener(Enum eventType, UnityAction action);
    void RemoveEventListener(Enum eventType, UnityAction<BaseEventData> action);
    void EventTrigger(Enum eventType);
    void EventTrigger(BaseEventData eventData);
    void Clear();
}