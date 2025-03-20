public class BehaviorTree
{
    public bool HaveRoot => root != null;
    private Behavior root;//根节点
    public BehaviorTree(Behavior root)
    {
        this.root = root;
    }
    public void Tick(bool repeated = true)
    {
        // 如果根节点已经成功/失败，直接不再调用
        if (root.IsTerminated && !repeated)
            return;
        root.Tick();
    }
    public void SetRoot(Behavior root)
    {
        this.root = root;
    }
}