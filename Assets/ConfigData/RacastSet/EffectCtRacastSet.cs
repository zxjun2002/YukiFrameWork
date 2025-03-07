using System.Collections.Generic;
using System.Linq;
public struct EffectCtRacastSet : IRacastSet
{
    public Dictionary<int, EffectCt> dic;

    public EffectCtRacastSet(ConfData data)
    {
        dic = data.effectCt.ToDictionary(es => es.id);
    }
}

