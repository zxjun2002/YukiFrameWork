using UnityEngine;

public class TimerNode : Behavior
{
    private readonly float waitTime;  // 等待时间（秒）
    private float startTime;

    public TimerNode(float waitTime)
    {
        this.waitTime = waitTime;
    }

    protected override void OnInitialize()
    {
        startTime = Time.time;  // 假设在 Unity 环境下使用
    }

    protected override EStatus OnUpdate()
    {
        if (Time.time - startTime >= waitTime)
            return EStatus.Success;
        return EStatus.Running;
    }
}

public partial class BehaviorTreeBuilder
{
    public BehaviorTreeBuilder TimerNode(int time)
    {
        var node = new TimerNode(time);
        AddBehavior(node);
        return this;
    }
}