using System.Collections.Generic;
using Domain;
using MIKUFramework.IOC;
using UnityEngine;
using UnityEngine.UI;

namespace Yuki
{
    public partial class AUIPanel : BasePanel
    {
        [Autowired] RedPointRepository redPointRepository;
        [Autowired] HttpAppService httpAppService;
        List<UIListItemData> TestUIList_ItemDatas = new List<UIListItemData>();//定义列表项数据List
        UIListItemData SetIndexData_Item(int idx)//定义函数,获取下标对应的数据
        {
            return TestUIList_ItemDatas[idx];
        }
        #region 生命周期
        public override void Init()
        {
            base.Init();
        }

        public override async void OnShow(BasePanelArg arg = null)
        {
            CheckBtn.onClick.AddListener(CheckBtnCallback);
            base.OnShow();
            //最后写入数据并且赋值
            TestUIList_ItemDatas.Clear();
            for (int i = 0; i < 100; i++)
            {
                var itemData = new Test_UIListItemData
                {
                    num = i
                };
                TestUIList_ItemDatas.Add(itemData);
            }
            TestUIList.SetIndexData = SetIndexData_Item;
            TestUIList.SetCount(TestUIList_ItemDatas.Count);
            await httpAppService.SendHttpReq(new Login_RequestHandler(114514, "Yuki"));
            redPointRepository.Agg.AddNode(RedPointKey.Play_LEVEL1_SHOP);
            redPointRepository.Agg.AddNode(RedPointKey.Play_LEVEL1_HOME);
            GameLogger.LogCyan(redPointRepository.Agg.GetRedpointNum(RedPointKey.Play_LEVEL1));
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
