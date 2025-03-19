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
        [Autowired] GuideRepository guideRepository;
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

        public override void OnShow(BasePanelArg arg = null)
        {
            CheckBtn.onClick.AddListener(CheckBtnCallback);
            base.OnShow();
            pool = new ObjectPool<GameObject>(() =>
                {
                    GameObject newInstance = Instantiate(Resources.Load("Cell/TestItem")) as GameObject;
                    return newInstance;
                },
                (obj) =>
                {
                    obj.SetActive(true);
                },
                (obj) =>
                {
                    obj.SetActive(false);
                },
                (obj) =>
                {
                    Destroy(obj);
                },true,30);
            // //最后写入数据并且赋值
            // TestUIList_ItemDatas.Clear();
            // for (int i = 0; i < 100; i++)
            // {
            //     var itemData = new Test_UIListItemData
            //     {
            //         Index = i,
            //         num = i,
            //         GuideAction = async go =>
            //         {
            //             BeginnerGuideDataList.SetGuideTarget(go);
            //             await UniTask.DelayFrame(1);
            //             guideRepository.AddGuide(BeginnerGuideDataList);
            //             guideRepository.PlayGuide();
            //         }
            //     };
            //     TestUIList_ItemDatas.Add(itemData);
            // }
            // TestUIList.SetIndexData = SetIndexData_Item;
            // TestUIList.SetCount(TestUIList_ItemDatas.Count);
            // // await httpAppService.SendHttpReq(new Login_RequestHandler(114514, "Yuki"));
            // redPointRepository.Agg.SetCallBack(RedPointKey.Play_LEVEL1, (node) =>
            // {
            //     GameLogger.LogGreen(node);
            // });
            // redPointRepository.Agg.AddNode(RedPointKey.Play_LEVEL1_SHOP);
            // redPointRepository.Agg.AddNode(RedPointKey.Play_LEVEL1_HOME);
            // 注册不同数据类型的预制体
            scrollerController.RegisterPrefab<HeaderBaseCellData>(headerPrefab);
            scrollerController.RegisterPrefab<ContentBaseCellData>(itemPrefab);

            // 构造数据
            List<BaseCellData> data = new List<BaseCellData>
            {
                new HeaderBaseCellData()
                {
                    title = "列表1"
                },
                new ContentBaseCellData()
                {
                    pool = pool,
                    dataIndexList = Enumerable.Range(0, 10).ToList()
                },
                new HeaderBaseCellData()
                {
                    title = "列表2"
                },
                new ContentBaseCellData()
                {
                    pool = pool,
                    dataIndexList = Enumerable.Range(0, 20).ToList()
                },
                new HeaderBaseCellData()
                {
                    title = "列表3"
                },
                new ContentBaseCellData()
                {
                    pool = pool,
                    dataIndexList = Enumerable.Range(0, 50).ToList()
                },
                new HeaderBaseCellData()
                {
                    title = "列表4"
                },
                new ContentBaseCellData()
                {
                    pool = pool,
                    dataIndexList = Enumerable.Range(0, 10).ToList()
                },
            };

            // 传入数据
            scrollerController.SetData(data);
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
