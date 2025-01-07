using System;

public abstract class BaseEventData
{
    public Enum EventType { get; protected set; }
    
    protected BaseEventData(Enum eventType)
    {
        EventType = eventType;
    }
}

public enum CustomEventType
{
    TestEventWithParam,
    TestEventWithInt,
    TestEventWithoutParam
}