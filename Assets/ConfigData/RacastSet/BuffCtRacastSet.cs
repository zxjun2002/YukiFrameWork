using System.Collections.Generic;
using System.Linq;
public struct BuffCtRacastSet : IRacastSet
{
    public Dictionary<int, BuffCtRacast> dic;
    public BuffCtRacastSet(ConfData data)
    {
        dic = (from es in data.buffCt
               let esr = new BuffCtRacast(es)
               select esr).ToDictionary(esr => esr.sourceConf.id);
    }
}
public class BuffCtRacast
{
    public BuffCt sourceConf;

    public BuffCtRacast(BuffCt sourceConf)
    {
        this.sourceConf = sourceConf;
    }
}

