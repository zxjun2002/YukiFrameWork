using System.Collections.Generic;
using System.Linq;
public struct ItemRacastSet : IRacastSet
{
    public Dictionary<int, ItemRacast> dic;
    public ItemRacastSet(ConfData data)
    {
        dic = (from es in data.item
               let esr = new ItemRacast(es)
               select esr).ToDictionary(esr => esr.sourceConf.id);
    }
}
public class ItemRacast
{
    public Item sourceConf;

    public ItemRacast(Item sourceConf)
    {
        this.sourceConf = sourceConf;
    }
}

