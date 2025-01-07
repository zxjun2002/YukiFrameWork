using System;
using System.Collections.Generic;
using MIKUFramework.IOC;

[Component]
public class ConfigTable: IConfigTable
{
    private ConfData confs;
    private Dictionary<Type, IRacastSet> racastSets;

    public void Init(string url)
    {
        confs = FileBytesUtil.LoadConfFromBinary<ConfData>(ResEditorConfig.ConfsAsset_Path);
        racastSets = new Dictionary<Type, IRacastSet>();
        //TODO：新增配置表需加入racastSets中
        racastSets.Add(typeof(ItemRacastSet), new ItemRacastSet(confs));
        
        GameLogger.Log("配置表初始化成功！！！");
    }

    public T GetConfig<T>() where T : struct, IRacastSet
    {
        racastSets.TryGetValue(typeof(T), out var config);
        return (T)config; // 进行类型转换
    }
}