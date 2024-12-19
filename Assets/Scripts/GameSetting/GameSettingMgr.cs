using Domain;
using MIKUFramework.IOC;
using UnityEngine;
using YuKi;

public class GameSettingMgr : InjectableMonoBehaviour
{
    [Autowired] private IUIManager uiManager;
    [Autowired] private PetRepository petRepository;
    /// <summary>
    /// 这是一个Unity特有的属性，用于指定Initialize方法作为运行时初始化方法，
    /// 在场景加载之前执行。这意味着，无论场景如何改变，Initialize方法都会在场景加载之前被调用一次。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        IoCHelper.Initialize();
    }
    
    protected override void OnStart()
    {
        petRepository.Init();
        petRepository.Aggs[1].PetInfo_E.SetNickname("NumberMan");
        uiManager.OpenPanel<AUIPanel>(new AUIPanelArg()
        {
            content = petRepository.Aggs[1].PetInfo_E.Nickname
        });
    }
}
