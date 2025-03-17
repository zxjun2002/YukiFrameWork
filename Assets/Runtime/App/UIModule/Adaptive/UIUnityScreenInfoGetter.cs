using UnityEngine;

public class UIUnityScreenInfoGetter : IUIScreenInfoGetter {

    public float GetDisTopNotSafeHeight() {
        // 实际屏幕中刘海屏高度
        float actualDisTop = Screen.height - Screen.safeArea.yMax;
        // 根据比例换算，换算出在参考高度中的高度
        var disTop = actualDisTop / Screen.height * IUIScreenInfoGetter.ReferenceHeight;
        return disTop;
    }

    public float GetDisBottomNotSafeHeight() {
        // 实际屏幕中底部不安全区域高度
        float actualDisBottom = Screen.safeArea.yMin;
        // 根据比例换算，换算出在参考高度中的高度
        var disBottom = actualDisBottom / Screen.height * IUIScreenInfoGetter.ReferenceHeight;
        return disBottom;
    }

    public bool HasNotch() {
        bool hasNotch = System.Math.Abs(Screen.height - Screen.safeArea.height) > 0;
        return hasNotch;
    }
}