using MIKUFramework.IOC;
using UnityEngine;

public class UINotchAdaptive : InjectableMonoBehaviour {
    [Autowired] private DeviceAppService _appService;
    public enum AdaptiveMode {
        None,
        MoveDown,  //向下移动
        MoveUp, //向上移动
        DownStretch, //下边缘向下拉伸
        UpStretch,//上边缘向上拉伸
        UpShorten,//下边缘向上缩短
        DownShorten,//上边缘向下缩短
        UpDownToInsideShorten, // 上下向内部缩短
        UpDownToOutsideStretch, // 上下向外部拉伸
    }

    public Vector2 anchoredPosition;
    public Vector2 sizeDelta;
    public Vector2 anchorMax;
    public Vector2 anchorMin;
    public Vector2 offsetMax;
    public Vector2 offsetMin;
    public float disTop { get;private set;}
    public float disBottom { get;private set;}

    public AdaptiveMode mode = AdaptiveMode.None;

    public void ApplicationAdaptive(Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 anchorMax, Vector2 anchorMin, Vector2 offsetMax, Vector2 offsetMin) {
        if (!_appService.screenAdapt.NotchAdaptive.HasNotch)
            return;

        disTop = _appService.screenAdapt.NotchAdaptive.DisTopNotSafeHeight;
        disBottom = _appService.screenAdapt.NotchAdaptive.DisBottomNotSafeHeight;


        RectTransform rect = GetComponent<RectTransform>();
        if (this.mode == AdaptiveMode.MoveDown) {
            if (anchorMin.y == anchorMax.y) {   //垂直方向上的锚点在同一个点
                //直接改PosY就好了
                rect.anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y - disTop);
            } else {
                rect.offsetMin = new Vector2(offsetMin.x, offsetMin.y - disTop);
                rect.offsetMax = new Vector2(offsetMax.x, offsetMax.y - disTop);
            }
        } else if (this.mode == AdaptiveMode.MoveUp) {
            if (anchorMin.y == anchorMax.y) {   //垂直方向上的锚点在同一个点
                //直接改PosY就好了
                rect.anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y + disBottom);
            } else {
                rect.offsetMin = new Vector2(offsetMin.x, offsetMin.y + disBottom);
                rect.offsetMax = new Vector2(offsetMax.x, offsetMax.y + disBottom);
            }
        } else if (this.mode == AdaptiveMode.DownStretch) {
            if (anchorMin.y == anchorMax.y) {   //垂直方向上的锚点在同一个点
                //直接改PosY就好了
                rect.sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y + disTop);
                float offsetY = (1 - rect.pivot.y) * disTop; //坐标向下移动的数值
                rect.anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y - offsetY);
            } else {
                rect.offsetMin = new Vector2(offsetMin.x, offsetMin.y - disTop);
                rect.offsetMax = new Vector2(offsetMax.x, offsetMax.y);
            }
        } else if (this.mode == AdaptiveMode.UpStretch) {
            if (anchorMin.y == anchorMax.y) {   //垂直方向上的锚点在同一个点
                //直接改PosY就好了
                rect.sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y + disBottom);
                float offsetY = rect.pivot.y * disBottom; //坐标向上移动的数值
                rect.anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y + offsetY);
            } else {
                rect.offsetMin = new Vector2(offsetMin.x, offsetMin.y);
                rect.offsetMax = new Vector2(offsetMax.x, offsetMax.y + disBottom);
            }
        } else if (this.mode == AdaptiveMode.UpShorten) {
            if (anchorMin.y == anchorMax.y) {   //垂直方向上的锚点在同一个点
                //直接改PosY就好了
                rect.sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y - disBottom);//下半部分缩小量
                float offsetY = (1 - rect.pivot.y) * disBottom; //坐标向下偏移量的数值
                rect.anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y + offsetY);//向上偏移量
            } else {
                rect.offsetMin = new Vector2(offsetMin.x, offsetMin.y + disBottom);//下半部分缩小量
                rect.offsetMax = new Vector2(offsetMax.x, offsetMax.y);//下半部分缩小量
            }
        } else if (this.mode == AdaptiveMode.DownShorten) {
            if (anchorMin.y == anchorMax.y) {   //垂直方向上的锚点在同一个点
                //直接改PosY就好了
                rect.sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y - disTop);//上半部分缩小量
                float offsetY = (1 - (1 - rect.pivot.y)) * disTop; //坐标向上偏移量的数值
                rect.anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y - offsetY);//向上偏移量
            } else {
                rect.offsetMin = new Vector2(offsetMin.x, offsetMin.y);//上半部分缩小量
                rect.offsetMax = new Vector2(offsetMax.x, offsetMax.y - disTop);//上半部分缩小量
            }

        } else if (this.mode == AdaptiveMode.UpDownToInsideShorten) {
            if (anchorMin.y == anchorMax.y) {   //垂直方向上的锚点在同一个点
                rect.sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y - disTop - disBottom);//上半部分缩小量
                float offsetDownY = (1 - (1 - rect.pivot.y)) * disTop; //坐标向上偏移量的数值
                float offsetUpY = (1 - rect.pivot.y) * disBottom; //坐标向下偏移量的数值
                rect.anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y - offsetDownY + offsetUpY);
            } else {
                rect.offsetMin = new Vector2(offsetMin.x, offsetMin.y + disBottom);
                rect.offsetMax = new Vector2(offsetMax.x, offsetMax.y - disTop);
            }
        } else if (this.mode == AdaptiveMode.UpDownToOutsideStretch) {
            if (anchorMin.y == anchorMax.y) {   //垂直方向上的锚点在同一个点
                rect.sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y + disBottom + disTop);
                float offsetUpY = rect.pivot.y * disBottom; //坐标向上移动的数值
                float offsetDownY = (1 - rect.pivot.y) * disTop; //坐标向下移动的数值
                rect.anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y + offsetUpY - offsetDownY);
            } else {
                rect.offsetMin = new Vector2(offsetMin.x, offsetMin.y - disTop);
                rect.offsetMax = new Vector2(offsetMax.x, offsetMax.y + disBottom);
            }
        } else {
            if (anchorMin.y == anchorMax.y) {   //垂直方向上的锚点在同一个点
                rect.sizeDelta = this.sizeDelta;
                rect.anchoredPosition = this.anchoredPosition;
            } else {
                rect.offsetMax = this.offsetMax;
                rect.offsetMin = this.offsetMin;
            }
        }
    }

    protected override void OnStart()
    {
        RectTransform rect = GetComponent<RectTransform>();
        this.anchoredPosition = rect.anchoredPosition;
        this.sizeDelta = rect.sizeDelta;
        this.anchorMax = rect.anchorMax;
        this.anchorMin = rect.anchorMin;
        this.offsetMax = rect.offsetMax;
        this.offsetMin = rect.offsetMin;

        this.ApplicationAdaptive(rect.anchoredPosition, rect.sizeDelta, rect.anchorMax, rect.anchorMin, rect.offsetMax, rect.offsetMin);
    }
}