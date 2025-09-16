using System.Collections.Generic;
using System.Linq;

public sealed partial class BuffRacastSet : IRacastSet
{
    public Dictionary<int, Dictionary<int, BuffCt>> BuffCtCt { get; private set; }
    public Dictionary<int, EffectCt> EffectCtCt { get; private set; }

    public BuffRacastSet(ConfData data)
    {
        BuffCtCt = data.buffCt.GroupBy(e => e.buffId).ToDictionary(g0 => g0.Key, g0 => g0.ToDictionary(e => e.buffLevel, e => e));
        EffectCtCt = data.effectCt.ToDictionary(e => e.id, e => e);
        OnAfterInit();
    }

    // 在 *.Logic.cs 中实现；未实现则无开销
    partial void OnAfterInit();
}
