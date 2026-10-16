using System.IO;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Domain;
using MIKUFramework.IOC;
using UnityEngine;
using Yuki;
using Yuki.SaveSystem;

public class GameSettingMgr : InjectableMonoBehaviour
{
    [Autowired] private IUIManager uiManager;
    [Autowired] private PetRepository petRepository;
    [Autowired] private IConfigTable configTable;
    [Autowired] private ISaveModule saveModule;
    [Autowired] private IEventCenter eventCenter;
    [Autowired] private DeviceAppService deviceAppService;
    [Autowired] private RedPointRepository redPointRepository;
    [Autowired] private IHttpModule httpModule;
    [Autowired] private HttpAppService httpAppService;
    [SerializeField] private Camera uiCamera;
    BehaviorTreeBuilder builder;
    /// <summary>
    /// 这是一个Unity特有的属性，用于指定Initialize方法作为运行时初始化方法，
    /// 在场景加载之前执行。这意味着，无论场景如何改变，Initialize方法都会在场景加载之前被调用一次。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        IoCHelper.Initialize();
    }
    
    private void Awake()
    {
        builder = new BehaviorTreeBuilder();
    }
    
    protected override async void OnStart()
    {
        deviceAppService.Init();
        eventCenter.AddEventListener(CustomEventType.TestEventWithParam,TestEventWithParam);
        eventCenter.AddEventListener(CustomEventType.TestEventWithoutParam,TestEventWithoutParam);
        string configPath = Path.Combine(Application.dataPath, ResEditorConfig.ConfsAsset_Path);
        configTable.Init(configPath);
        InitSaveData();
        petRepository.Init();
        redPointRepository.Init();
        httpAppService.Init();
        httpModule.Init(HttpConfig.GameUrl);
        Lean.Localization.LeanLocalization.SetCurrentLanguageAll("English");
        petRepository.Aggs[1].PetInfo_E.SetNickname(configTable.GetConfig<ItemRacastSet>().ItemCt[1001].itemTestList[0].ToString());
        GameLogger.LogCyan(configTable.GetConfig<ItemRacastSet>().ItemCt[1001].itemTestList[0].ToString());
        GameLogger.LogGreen(configTable.GetConfig<BuffRacastSet>().EffectCtCt[102].effectVal);
        GameLogger.LogYellow(Lean.Localization.LeanLocalization.GetTranslationText("Title"));//获取key的翻译);
        await UniTask.DelayFrame(500);
        eventCenter.EventTrigger(new StringEventData(CustomEventType.TestEventWithParam,configTable.GetConfig<BuffRacastSet>().GetBuffCt(101,1).buffName));
        eventCenter.EventTrigger(CustomEventType.TestEventWithoutParam);
        uiManager.Init(uiCamera);
        uiManager.OpenPanel<AUIPanel>();
        builder.Repeat(3)
            .Sequence()
                .DebugNode("Ok,")//由于动作节点不进栈，所以不用Back
                .DebugNode("It's ")
                .DebugNode("My time")
                .TimerNode(1)
            .Back()
            .End();
        await foreach (var _ in UniTaskAsyncEnumerable.EveryUpdate(PlayerLoopTiming.Update).WithCancellation(this.GetCancellationTokenOnDestroy()))
        {
            builder.TreeTick(false);
        }
    }
    
    private void InitSaveData()
    {
        var database = saveModule.Open();
        var players = database.Table<Player>();
        var inventory = database.Table<InventoryTable>();

        database.Transaction(_ =>
        {
            // 只补充缺少的示例记录，重复启动不会覆盖已修改的数据。
            if (players.Find(1) == null)
                players.Save(new Player { Id = 1, Name = "Yuki", Level = 5, Gold = 1000 });

            var items = new[]
            {
                new InventoryTable { Id = 1, PlayerId = 1, ItemId = 1001, Name = "生命药水", Count = 5 },
                new InventoryTable { Id = 2, PlayerId = 1, ItemId = 1002, Name = "魔法药水", Count = 3 },
                new InventoryTable { Id = 3, PlayerId = 1, ItemId = 1003, Name = "铁剑", Count = 1 }
            };
            foreach (var item in items)
                if (inventory.Find(item.Id) == null) inventory.Save(item);
        });

        var player = players.Find(1);
        GameLogger.LogCyan($"[存档] 文件路径：{database.FilePath}");
        GameLogger.Log($"[存档] Player：{player.Name}，等级 {player.Level}，金币 {player.Gold}");
        GameLogger.Log($"[存档] InventoryTable：{inventory.Count()} 条记录");
    }

    private void TestEventWithParam(BaseEventData eventData)
    {
        if (eventData is StringEventData data)
        {
            GameLogger.LogGreen(data.Message);
        }
    }
    
    private void TestEventWithoutParam()
    {
        GameLogger.LogGreen("TestEventWithoutParam");
    }

}
