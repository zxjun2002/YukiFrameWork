

using System;

public class StringEventData : BaseEventData
{
    public string Message { get; private set; }

    public StringEventData(Enum eventType, string message) : base(eventType)
    {
        Message = message;
    }
}

public class IntEventData : BaseEventData
{
    public int Value { get; private set; }

     public IntEventData(Enum eventType, int value) : base(eventType)
    {
        Value = value;
    }
}
