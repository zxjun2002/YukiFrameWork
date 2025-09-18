using System;

public interface IEventCenter
{
    void AddEventListener(Enum eventType, Action action);
    void AddEventListener(Enum eventType, Action<BaseEventData> action);
    void RemoveEventListener(Enum eventType, Action action);
    void RemoveEventListener(Enum eventType, Action<BaseEventData> action);
    void EventTrigger(Enum eventType);
    void EventTrigger(BaseEventData eventData);
    void Clear();
}