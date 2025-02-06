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
            if (Agg == null)
            {
                Agg = new RedPointAgg();
            }
        }
    }
}