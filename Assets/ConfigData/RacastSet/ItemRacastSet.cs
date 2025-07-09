using System.Collections.Generic;
using System.Linq;

public struct ItemRacastSet : IRacastSet
{
    public Dictionary<int, Item> ItemCt { get; private set; }
    public Dictionary<int, TestItem> TestItemCt { get; private set; }

    public ItemRacastSet(ConfData data)
    {
        ItemCt = data.item.ToDictionary(es => es.id);
        TestItemCt = data.testItem.ToDictionary(es => es.id);
    }
}
