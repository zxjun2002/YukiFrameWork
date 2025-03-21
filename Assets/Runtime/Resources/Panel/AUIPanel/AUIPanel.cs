using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
        [Autowired] GuideRepository guideRepository;
        [Autowired] IUIManager uiManager;
        [SerializeField] private UIBeginnerGuideDataList BeginnerGuideDataList;
        List<UIListItemData> TestUIList_ItemDatas = new List<UIListItemData>();//定义列表项数据List
        private ObjectPool<GameObject> pool;
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
            base.OnShow();
            CheckBtn.onClick.AddListener(CheckBtnCallback);
            //最后写入数据并且赋值
            TestUIList_ItemDatas.Clear();
            for (int i = 0; i < 100; i++)
            {
                UIListItemData itemData = null;
                switch (i % 2)
                {
                    case 0:
                        itemData = new Test_UIListItemData
                        {
                            Index = i,
                            num = i,
                            GuideAction = async go =>
                            {
                                BeginnerGuideDataList.SetGuideTarget(go);
                                await UniTask.DelayFrame(1);
                                guideRepository.AddGuide(BeginnerGuideDataList);
                                guideRepository.PlayGuide();
                            }
                        };
                        break;
                    case 1:
                        itemData = new TestMulti_UIListItemData()
                        {
                            num = i,
                        };
                        break;
                }
                TestUIList_ItemDatas.Add(itemData);
            }
            
            TestUIListMulti.SetIndexData = SetIndexData_Item;
            TestUIListMulti.SetCount(TestUIList_ItemDatas.Count);
            // TestUIList.SetIndexData = SetIndexData_Item;
            // TestUIList.SetCount(TestUIList_ItemDatas.Count);
            // // await httpAppService.SendHttpReq(new Login_RequestHandler(114514, "Yuki"));
            // redPointRepository.Agg.SetCallback(RedPointKey.Play_LEVEL1,Play_LEVELRedDotCallback);
            //
            // // 构造数据
            // List<BaseCellData> data = new List<BaseCellData>
            // {
            //     new HeaderCellData()
            //     {
            //         title = "列表1"
            //     },
            //     new ContentCellData()
            //     {
            //         dataIndexList = Enumerable.Range(0, 10).ToList()
            //     },
            //     new HeaderCellData()
            //     {
            //         title = "列表2"
            //     },
            //     new ContentCellData()
            //     {
            //         dataIndexList = Enumerable.Range(0, 20).ToList()
            //     },
            //     new HeaderCellData()
            //     {
            //         title = "列表3"
            //     },
            //     new ContentCellData()
            //     {
            //         dataIndexList = Enumerable.Range(0, 50).ToList()
            //     },
            //     new HeaderCellData()
            //     {
            //         title = "列表4"
            //     },
            //     new ContentCellData()
            //     {
            //         dataIndexList = Enumerable.Range(0, 10).ToList()
            //     },
            //     new HeaderCellData()
            //     {
            //         title = "列表5"
            //     },
            //     new ContentCellData()
            //     {
            //         dataIndexList = Enumerable.Range(0, 40).ToList()
            //     },
            // };
            // // 传入数据
            // scrollerController.SetData(data);
        }

        public override void OnClose()
        {
            base.OnClose();
            CheckBtn.onClick.RemoveListener(CheckBtnCallback);
            redPointRepository.Agg.DeleteCallback(RedPointKey.Play_LEVEL1, Play_LEVELRedDotCallback);
        }
        #endregion

        #region 控件回调
        void CheckBtnCallback()
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
