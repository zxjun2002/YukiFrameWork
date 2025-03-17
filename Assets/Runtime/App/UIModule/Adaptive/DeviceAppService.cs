using MIKUFramework.IOC;


[Component]
public class DeviceAppService
{
    public ScreenAdapt screenAdapt{get;private set;}
    #region 设备初始化

    public void Init()
    {
        screenAdapt = new ScreenAdapt();
        screenAdapt.InitializeScreenAdaptive();
    }
    #endregion
}