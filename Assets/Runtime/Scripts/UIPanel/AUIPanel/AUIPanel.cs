using UnityEngine;
using UnityEngine.UI;

namespace YuKi
{
    public partial class AUIPanel : BasePanel
    {
        #region 生命周期
        public override void Init()
        {
            base.Init();
        }

        public override void OnShow(BasePanelArg arg = null)
        {
            CheckBtn.onClick.AddListener(CheckBtnCallback);
            base.OnShow();
        }

        public override void OnClose()
        {
            CheckBtn.onClick.RemoveListener(CheckBtnCallback);
            base.OnClose();
        }
        #endregion

        #region 控件回调
        void CheckBtnCallback()
        {
            // TODO: Add your logic here
        }
        #endregion
    }
}
