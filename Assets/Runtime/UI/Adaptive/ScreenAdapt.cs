public class ScreenAdapt
{
    private IUIScreenInfoGetter _screenInfoGetter;
    UIScreenAdaptiveInfo notchAdaptive;
    public UIScreenAdaptiveInfo NotchAdaptive {
        get {
            if (notchAdaptive == null) {
                notchAdaptive = new UIScreenAdaptiveInfo();
            }
            return notchAdaptive;
        }
    }

    // 初始化
    public void InitializeScreenAdaptive() {
#if !UNITY_EDITOR && UNITY_WEBGL
        _screenInfoGetter = new UIWXScreenInfoGetter();
#else
        _screenInfoGetter = new UIUnityScreenInfoGetter();
#endif
        NotchAdaptive.InitScreenInfo(this._screenInfoGetter.GetDisTopNotSafeHeight(), this._screenInfoGetter.GetDisBottomNotSafeHeight(), this._screenInfoGetter.HasNotch());
    }
}