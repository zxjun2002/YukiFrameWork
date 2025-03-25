using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Yuki
{
    public partial class AUIPanel
    {
        [GroupDropdownStart("UI Elements")]
        [SerializeField] private Image TestImg;
        [SerializeField] private TextMeshProUGUI NameText;
        [SerializeField] private Button CheckBtn;
        [GroupDropdownEnd("UI Elements")]

        [GroupDropdownStart("Data List")]
        [SerializeField] private UIList TestUIList;
        [SerializeField] private EnhancedScrollerController scrollerController;
        [SerializeField] private UIListMulti TestUIListMulti;
        [GroupDropdownEnd("Data List")]

        // 没有分组的字段，就按默认方式显示
        [SerializeField] private int SomeOtherField;
    }
}
