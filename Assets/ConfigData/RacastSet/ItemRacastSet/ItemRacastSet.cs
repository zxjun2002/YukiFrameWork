using System.Collections.Generic;
using System.Linq;

public sealed partial class ItemRacastSet : IRacastSet
{
    public Dictionary<int, Item> ItemCt { get; private set; }
    public Dictionary<int, TestItem> TestItemCt { get; private set; }

    public ItemRacastSet(ConfData data)
    {
        ItemCt = data.item.ToDictionary(e => e.id, e => e);
        TestItemCt = data.testItem.ToDictionary(e => e.id, e => e);
        OnAfterInit();
    }

    // 在 *.Logic.cs 中实现；未实现则无开销
    partial void OnAfterInit();
}
