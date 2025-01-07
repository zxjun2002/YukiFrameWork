

public class StringEventData : BaseEventData
{
    public string Message { get; private set; }

    public StringEventData(CustomEventType eventType, string message) : base(eventType)
    {
        Message = message;
    }
}

public class IntEventData : BaseEventData
{
    public int Value { get; private set; }

     public IntEventData(CustomEventType eventType, int value) : base(eventType)
    {
        Value = value;
    }
}