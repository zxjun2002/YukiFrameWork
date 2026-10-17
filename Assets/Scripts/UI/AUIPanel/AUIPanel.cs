using UnityEngine;

namespace Yuki
{
    public partial class AUIPanel : BasePanel
    {
        #region 生命周期
        public override void OnShow(BasePanelArg arg = null)
        {
            base.OnShow(arg);
            BindUIEvents();
        }

        public override void OnClose()
        {
            UnbindUIEvents();
            base.OnClose();
        }
        #endregion

        void CheckBtn_Button_onClick()
        {
            // TODO: Add your logic here
            Debug.Log("CheckBtn_Button_onClick_9260c0d7");
        }
    }
}
