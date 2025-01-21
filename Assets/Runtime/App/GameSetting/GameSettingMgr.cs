using Cysharp.Threading.Tasks;
using Domain;
using MIKUFramework.IOC;
using UnityEngine;
using YuKi;

public class GameSettingMgr : InjectableMonoBehaviour
{
    [Autowired] private IUIManager uiManager;
    [Autowired] private PetRepository petRepository;
    [Autowired] private IConfigTable configTable;
    [Autowired] private IEventCenter eventCenter;
    [Autowired] private DeviceAppService deviceAppService;
    /// <summary>
    /// 这是一个Unity特有的属性，用于指定Initialize方法作为运行时初始化方法，
    /// 在场景加载之前执行。这意味着，无论场景如何改变，Initialize方法都会在场景加载之前被调用一次。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        IoCHelper.Initialize();
    }
    
    protected override async void OnStart()
    {
        deviceAppService.Init();
        eventCenter.AddEventListener(CustomEventType.TestEventWithParam,TestEventWithParam);
        eventCenter.AddEventListener(CustomEventType.TestEventWithoutParam,TestEventWithoutParam);
        configTable.Init(ResEditorConfig.ConfsAsset_Path);
        petRepository.Init();
        petRepository.Aggs[1].PetInfo_E.SetNickname(configTable.GetConfig<ItemRacastSet>().dic[1001].sourceConf.itemName);
        GameLogger.LogGreen(configTable.GetConfig<EffectCtRacastSet>().dic[102].sourceConf.effectVal);
        await UniTask.DelayFrame(500);
        eventCenter.EventTrigger(new StringEventData(CustomEventType.TestEventWithParam,configTable.GetConfig<BuffCtRacastSet>().dic[102].sourceConf.buffName));
        eventCenter.EventTrigger(CustomEventType.TestEventWithoutParam);
        uiManager.OpenPanel<AUIPanel>();
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
