using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Domain;
using MIKUFramework.IOC;
using UnityEngine;
using Yuki;

public class GameSettingMgr : InjectableMonoBehaviour
{
    [Autowired] private IUIManager uiManager;
    [Autowired] private PetRepository petRepository;
    [Autowired] private IConfigTable configTable;
    [Autowired] private IEventCenter eventCenter;
    [Autowired] private DeviceAppService deviceAppService;
    [Autowired] private RedPointRepository redPointRepository;
    [Autowired] private IHttpModule httpModule;
    [Autowired] private HttpAppService httpAppService;
    [Autowired] private GuideRepository guideRepository;
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
        guideRepository.Init(transform.GetComponent<UIBeginnerGuideManager>());
        deviceAppService.Init();
        eventCenter.AddEventListener(CustomEventType.TestEventWithParam,TestEventWithParam);
        eventCenter.AddEventListener(CustomEventType.TestEventWithoutParam,TestEventWithoutParam);
        configTable.Init(ResEditorConfig.ConfsAsset_Path);
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
        eventCenter.EventTrigger(new StringEventData(CustomEventType.TestEventWithParam,configTable.GetConfig<BuffRacastSet>().BuffCtCt[102].buffName));
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
