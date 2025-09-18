using System.Collections.Generic;
using MIKUFramework.IOC;

namespace Domain
{
    [Component]
    public class GuideRepository
    {
        private GuideAgg Agg { get; set; }

        public void Init(UIBeginnerGuideManager guideManager)
        {
            Agg ??= new GuideAgg(guideManager);
        }

        public void AddGuide(UIBeginnerGuideDataList datalist)
        {
            Agg._guideManager.AddGuideList(datalist);
        }

        public void PlayGuide()
        {
            Agg._guideManager.ShowGuideList();
        }
        
        public void Clear()
        {
            Agg = null;
        }
    }
}