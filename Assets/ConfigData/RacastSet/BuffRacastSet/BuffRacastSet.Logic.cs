// 自定义扩展：此文件仅首次生成，之后不会被覆盖
using System.Collections.Generic;
using System.Linq;

public sealed partial class BuffRacastSet
{
    partial void OnAfterInit()
    {
        // TODO: 在这里构建你的业务索引/缓存
    }
    
    public BuffCt GetBuffCt(int buffId, int buffLevel)
    {
        return BuffCtCt[buffId][buffLevel];
    }
}
