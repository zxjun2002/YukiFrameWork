using System.Collections.Generic;
using MIKUFramework.IOC;

namespace Domain
{
    [Component]
    public class PetRepository
    {
        public Dictionary<int, PetAgg> Aggs { get; private set; }

        public void Init()
        {
            Aggs ??= new Dictionary<int, PetAgg>();

            for (int i = 1; i <= 3; i++)
            {
                Aggs.Add(i, new PetAgg(11000+i));
            }
        }

        public void Clear()
        {
            Aggs.Clear();
        }
    }
}