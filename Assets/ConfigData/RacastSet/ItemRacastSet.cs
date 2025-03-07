using System.Collections.Generic;
using System.Linq;
public struct ItemRacastSet : IRacastSet
{
    public Dictionary<int, Item> dic;

    public ItemRacastSet(ConfData data)
    {
        dic = data.item.ToDictionary(es => es.id);
    }
}

