using System.Collections.Generic;
using System.Linq;
public struct EffectCtRacastSet : IRacastSet
{
    public Dictionary<int, EffectCtRacast> dic;
    public EffectCtRacastSet(ConfData data)
    {
        dic = (from es in data.effectCt
               let esr = new EffectCtRacast(es)
               select esr).ToDictionary(esr => esr.sourceConf.id);
    }
}
public class EffectCtRacast
{
    public EffectCt sourceConf;

    public EffectCtRacast(EffectCt sourceConf)
    {
        this.sourceConf = sourceConf;
    }
}

