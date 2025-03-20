using System.Collections.Generic;

public partial class BehaviorTreeBuilder
{
    private readonly Stack<Behavior> nodeStack;
    private readonly BehaviorTree bhTree;
    public BehaviorTreeBuilder()
    {
        bhTree = new BehaviorTree(null);
        nodeStack = new Stack<Behavior>();
    }
    private void AddBehavior(Behavior behavior)
    {
        if (bhTree.HaveRoot)
        {
            nodeStack.Peek().AddChild(behavior);
        }
        else
        {
            bhTree.SetRoot(behavior);
        }
        if (behavior is Composite || behavior is Decorator)
        {
            nodeStack.Push(behavior);
        }
    }
    /// <summary>
    /// 运行树
    /// </summary>
    /// <param name="repeated">运行结束后是否回到根节点继续运行</param>
    public void TreeTick(bool repeated = true)
    {
        bhTree.Tick(repeated);
    }
    public BehaviorTreeBuilder Back()
    {
        nodeStack.Pop();
        return this;
    }
    public BehaviorTree End()
    {
        nodeStack.Clear();
        return bhTree;
    }
    //---------包装各节点---------
    public BehaviorTreeBuilder Sequence()
    {
        var tp = new Sequence();
        AddBehavior(tp);
        return this;
    }
    public BehaviorTreeBuilder Seletctor()
    {
        var tp = new Selector();
        AddBehavior(tp);
        return this;
    }
    public BehaviorTreeBuilder Filter()
    {
        var tp = new Filter();
        AddBehavior(tp);
        return this;
    }
    public BehaviorTreeBuilder Parallel(Parallel.Policy success, Parallel.Policy failure)
    {
        var tp = new Parallel(success, failure);
        AddBehavior(tp);
        return this;
    }
    public BehaviorTreeBuilder Monitor(Parallel.Policy success, Parallel.Policy failure)
    {
        var tp = new Monitor(success, failure);
        AddBehavior(tp);
        return this;
    }
    public BehaviorTreeBuilder ActiveSelector()
    {
        var tp = new ActiveSelector();
        AddBehavior(tp);
        return this;
    }
    public BehaviorTreeBuilder Repeat(int limit)
    {
        var tp = new Repeat(limit);
        AddBehavior(tp);
        return this;
    }
    public BehaviorTreeBuilder Inverter()
    {
        var tp = new Inverter();
        AddBehavior(tp);
        return this;
    }
}