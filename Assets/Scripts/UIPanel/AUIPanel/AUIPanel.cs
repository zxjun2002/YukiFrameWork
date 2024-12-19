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
            TestBtn.onClick.AddListener(TestBtnCallback);
            CheckBtn.onClick.AddListener(CheckBtnCallback);
            if (arg is AUIPanelArg panelArg)
            {
                NameText.text = panelArg.content;
            }
            base.OnShow();
        }

        public override void OnClose()
        {
            TestBtn.onClick.RemoveListener(TestBtnCallback);
            CheckBtn.onClick.RemoveListener(CheckBtnCallback);
            base.OnClose();
        }
        #endregion

        #region 控件回调
        void TestBtnCallback()
        {
            // TODO: Add your logic here
        }
        void CheckBtnCallback()
        {
            // TODO: Add your logic here
        }
        #endregion
    }

    public class AUIPanelArg : BasePanelArg
    {
        public string content;
    }
}
