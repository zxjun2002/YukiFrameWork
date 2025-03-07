using System.Collections.Generic;
using System.Linq;
public struct BuffCtRacastSet : IRacastSet
{
    public Dictionary<int, BuffCt> dic;

    public BuffCtRacastSet(ConfData data)
    {
        dic = data.buffCt.ToDictionary(es => es.id);
    }
}

