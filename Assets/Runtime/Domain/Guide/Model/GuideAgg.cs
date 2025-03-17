using System.Collections.Generic;
using UnityEngine;

namespace Domain
{
    /// <summary>
    /// 红点
    /// </summary>
    public class GuideAgg
    {
        public UIBeginnerGuideManager _guideManager;

        public GuideAgg(UIBeginnerGuideManager guideManager)
        {
            _guideManager = guideManager;
        }
    }
}