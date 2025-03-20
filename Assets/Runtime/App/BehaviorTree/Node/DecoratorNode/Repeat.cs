/// <summary>
/// 重复节点
/// </summary>
public class Repeat : Decorator
{
    private int conunter;//当前重复次数
    private int limit;//重复限度
    public Repeat(int limit)
    {
        this.limit = limit;
    }
    protected override void OnInitialize()
    {
        conunter = 0;//进入时，将次数清零
    }
    protected override EStatus OnUpdate()
    {
        EStatus result = child.Tick();
        if(result == EStatus.Running)
            return EStatus.Running;
        if(result == EStatus.Failure)
            return EStatus.Failure;
        
        // 子节点执行成功
        conunter++;
        if(conunter >= limit)
            return EStatus.Success;
    
        // 重置子节点以便下一次执行
        child.Reset();
        return EStatus.Running;
    }
}