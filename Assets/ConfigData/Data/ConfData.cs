/// <summary>
/// 此代码为自动生成,修改无意义重新生成会被后覆盖
/// </summary>

using MemoryPack;
using System.Collections.Generic;

[MemoryPackable]
public partial class ConfData{
    public Item[] item;
    public TestItem[] testItem;
    public BuffCt[] buffCt;
    public EffectCt[] effectCt;
}

[MemoryPackable]
public partial class Item {
    public int id;
    public string itemName;
    public long itemPid;
    public string itemJson;
    public int[] itemList;
    public List<int> itemTestList;
}

[MemoryPackable]
public partial class TestItem {
    public int id;
    public string itemDes;
}

[MemoryPackable]
public partial class BuffCt {
    public int id;
    public string buffName;
    public float buffVal;
}

[MemoryPackable]
public partial class EffectCt {
    public int id;
    public string effectName;
    public float effectVal;
}

