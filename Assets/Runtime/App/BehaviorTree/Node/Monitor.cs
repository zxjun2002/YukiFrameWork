/// <summary>
/// 监视节点
/// </summary>
public class Monitor: Parallel
{
    public Monitor(Policy mSuccessPolicy, Policy mFailurePolicy)
        : base(mSuccessPolicy, mFailurePolicy)
    {
    }
    public void AddCondition(Behavior condition)
    {
        children.AddFirst(condition);
    }
    public void AddAction(Behavior action)
    {
        children.AddLast(action);
    }
}