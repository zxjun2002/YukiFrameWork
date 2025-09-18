using System.Collections.Generic;
using MIKUFramework.IOC;

namespace Domain
{
    [Component]
    public class RedPointRepository
    {
        public RedPointAgg Agg { get; private set; }

        public void Init()
        {
            Agg ??= new RedPointAgg();
        }

        public void Clear()
        {
            Agg = null;
        }
    }
}