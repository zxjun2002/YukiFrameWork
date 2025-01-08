public class UIScreenAdaptiveInfo {

    public float DisTopNotSafeHeight { private set; get; }

    public float DisBottomNotSafeHeight { private set; get; }

    public bool HasNotch { private set; get; }

    public void InitScreenInfo(float disTopNotSafeHeight, float disBottomNotSafeHeight, bool hasNotch) {
        this.DisTopNotSafeHeight = disTopNotSafeHeight;
        this.DisBottomNotSafeHeight = disBottomNotSafeHeight;
        this.HasNotch = hasNotch;
    }
}