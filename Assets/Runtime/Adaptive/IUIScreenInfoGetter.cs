public interface IUIScreenInfoGetter {

    //制作参考高度
    public static float ReferenceHeight = 1334;

    // 获取顶部不安全区域的高度
    float GetDisTopNotSafeHeight();
    // 获取底部不安全区域的高度
    float GetDisBottomNotSafeHeight();

    bool HasNotch();
}