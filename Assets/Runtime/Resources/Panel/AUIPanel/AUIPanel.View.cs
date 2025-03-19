using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Yuki
{
    public partial class AUIPanel
    {
        [SerializeField] private Image TestImg;
        [SerializeField] private TextMeshProUGUI NameText;
        [SerializeField] private Button CheckBtn;
        [SerializeField] private UIList TestUIList;
        [SerializeField] private EnhancedScrollerController scrollerController;
        [SerializeField] private HeaderCellView headerPrefab;
        [SerializeField] private ContentCellView itemPrefab;
    }
}
