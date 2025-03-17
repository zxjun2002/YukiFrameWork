using System.Collections.Generic;
using MIKUFramework.IOC;

namespace Domain
{
    [Component]
    public class GuideRepository
    {
        public GuideAgg Agg { get; private set; }

        public void Init(UIBeginnerGuideManager guideManager)
        {
            if (Agg == null)
            {
                Agg = new GuideAgg(guideManager);
            }
        }

        public void AddGuide(UIBeginnerGuideDataList datalist)
        {
            Agg._guideManager.AddGuideList(datalist);
        }

        public void PlayGuide()
        {
            Agg._guideManager.ShowGuideList();
        }
    }
}