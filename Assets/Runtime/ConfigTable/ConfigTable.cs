using System;
using System.Collections.Generic;
using MIKUFramework.IOC;

[Component]
public class ConfigTable: IConfigTable
{
    private ConfData confs;
    private Dictionary<Type, Lazy<IRacastSet>> racastSets;

    public void Init(string url)
    {
        confs = FileBytesUtil.LoadConfFromBinary<ConfData>(ResEditorConfig.ConfsAsset_Path);
        //TODO：新增配置表需加入racastSets中
        racastSets = new Dictionary<Type, Lazy<IRacastSet>>
        {
            { typeof(ItemRacastSet),     new Lazy<IRacastSet>(() => new ItemRacastSet(confs)) },
            { typeof(BuffCtRacastSet),   new Lazy<IRacastSet>(() => new BuffCtRacastSet(confs)) },
            { typeof(EffectCtRacastSet), new Lazy<IRacastSet>(() => new EffectCtRacastSet(confs)) },
        };        
        GameLogger.Log("配置表初始化成功！！！");
    }

    public T GetConfig<T>() where T : struct, IRacastSet
    {
        var type = typeof(T);
        if (!racastSets.TryGetValue(type, out var lazy))
            throw new KeyNotFoundException($"未注册配置类型：{type.Name}");
        return (T)lazy.Value; // 第一次访问时才 new，以后直接复用
    }
}