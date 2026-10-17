using System.Collections.Generic;
using System.Linq;
using Domain;
using EnhancedUI.EnhancedScroller;
using MIKUFramework.IOC;
using UnityEngine;
using UnityEngine.Pool;

namespace Yuki
{
    public partial class AUIPanel : BasePanel
    {
        [Autowired] RedPointRepository redPointRepository;
        [Autowired] HttpAppService httpAppService;
        [Autowired] IUIManager uiManager;
        
        List<UIListItemData> TestUIList_ItemDatas = new List<UIListItemData>();//定义列表项数据List
        private ObjectPool<GameObject> pool;
        UIListItemData SetIndexData_Item(int idx)//定义函数,获取下标对应的数据
        {
            return TestUIList_ItemDatas[idx];
        }
        #region 生命周期
        public override void Init(Camera uiCamera)
        {
            base.Init(uiCamera);
        }

        public override void OnShow(BasePanelArg arg = null)
        {
            base.OnShow(arg);
            BindUIEvents();
            //最后写入数据并且赋值
            TestUIList_ItemDatas.Clear();
            // for (int i = 0; i < 100; i++)
            // {
            //     UIListItemData itemData = new Test_UIListItemData
            //     {
            //         Index = i,
            //         num = i,
            //         GuideAction = null
            //     };
            //     TestUIList_ItemDatas.Add(itemData);
            // }
            // TestUIList_UIList.SetIndexData = SetIndexData_Item;
            // TestUIList_UIList.SetCount(TestUIList_ItemDatas.Count);
            
            // for (int i = 0; i < 100; i++)
            // {
            //     UIListItemData itemData = null;
            //     switch (i % 2)
            //     {
            //         case 0:
            //             itemData = new Test_UIListItemData
            //             {
            //                 Index = i,
            //                 num = i,
            //                 GuideAction = null
            //             };
            //             break;
            //         case 1:
            //             itemData = new TestMulti_UIListItemData()
            //             {
            //                 num = i,
            //             };
            //             break;
            //     }
            //     TestUIList_ItemDatas.Add(itemData);
            // }
            //
            // TestMultiUIList_UIListMulti.SetIndexData = SetIndexData_Item;
            // TestMultiUIList_UIListMulti.SetCount(TestUIList_ItemDatas.Count);
            
            // // await httpAppService.SendHttpReq(new Login_RequestHandler(114514, "Yuki"));
            // redPointRepository.Agg.SetCallback(RedPointKey.Play_LEVEL1,Play_LEVELRedDotCallback);
            
            //构造数据
            List<BaseCellData> data = new List<BaseCellData>
            {
                new HeaderCellData()
                {
                    title = "列表1"
                },
                new ContentCellData()
                {
                    dataIndexList = Enumerable.Range(0, 10).ToList()
                },
                new HeaderCellData()
                {
                    title = "列表2"
                },
                new ContentCellData()
                {
                    dataIndexList = Enumerable.Range(0, 20).ToList()
                },
                new HeaderCellData()
                {
                    title = "列表3"
                },
                new ContentCellData()
                {
                    dataIndexList = Enumerable.Range(0, 50).ToList()
                },
                new HeaderCellData()
                {
                    title = "列表4"
                },
                new ContentCellData()
                {
                    dataIndexList = Enumerable.Range(0, 10).ToList()
                },
                new HeaderCellData()
                {
                    title = "列表5"
                },
                new ContentCellData()
                {
                    dataIndexList = Enumerable.Range(0, 40).ToList()
                },
            };
            // 传入数据
            Scroller_EnhancedScrollerController.SetData(data);
        }

        public override void OnClose()
        {
            base.OnClose();
            UnbindUIEvents();
            redPointRepository.Agg.DeleteCallback(RedPointKey.Play_LEVEL1, Play_LEVELRedDotCallback);
        }
        #endregion

        #region 控件回调
        void CheckBtn_Button_onClick()
        {
            uiManager.ClosePanel<AUIPanel>();
        }
        #endregion

        #region 事件回调
        void Play_LEVELRedDotCallback(int num)
        {
            GameLogger.LogGreen(num);
        }
        #endregion
    }
}
