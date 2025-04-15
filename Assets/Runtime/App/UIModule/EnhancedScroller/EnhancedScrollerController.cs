using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using EnhancedUI.EnhancedScroller;

/// <summary>
/// EnhancedScroller 的通用封装，适用于任何类型的 BaseCellData。
/// 不再直接存储 HeaderPrefab、ItemPrefab，而是外部注册类型 -> 预制体的映射。
/// </summary>
public class EnhancedScrollerController : MonoBehaviour, IEnhancedScrollerDelegate
{
    private EnhancedScroller scroller;
    
    // 用于控制列表方向
    private EnhancedScroller.ScrollDirectionEnum orientation;

    // 维护数据
    private List<BaseCellData> dataList = new List<BaseCellData>();

    [SerializeField] private List<EnhancedScrollerCellView> ScrollerCellViews;
    
    private void Awake()
    {
        scroller = gameObject.GetComponent<EnhancedScroller>();
        orientation = scroller.scrollDirection;
        // 订阅回收事件
        scroller.cellViewWillRecycle += OnCellViewWillRecycle;
    }

    private void OnDestroy()
    {
        if (scroller != null)
        {
            scroller.cellViewWillRecycle -= OnCellViewWillRecycle;
        }
    }

    private void OnCellViewWillRecycle(EnhancedScrollerCellView cellView)
    {
        // 当 cellView 被回收前调用 HideData
        cellView.HideData();
    }

    /// <summary>
    /// 设置 EnhancedScroller 的数据列表，并刷新显示。
    /// </summary>
    public void SetData(List<BaseCellData> newData)
    {
        dataList = newData ?? new List<BaseCellData>();

        if (scroller != null)
        {
            scroller.Delegate = this;
            scroller.ReloadData(); // 重新加载数据
        }
    }
    
    /// <summary>
    /// 设置列表滚动速度
    /// </summary>
    /// <param name="amount"></param>
    public void AddVelocity(float amount)
    {
        scroller.LinearVelocity = amount;
    }
    
    /// <summary>
    /// 老虎机转到指定数据下标
    /// </summary>
    /// <param name="targetIndex">指定数据下标</param>
    /// <param name="loops">循环圈数</param>
    /// <param name="tweenTime">补间动画时间,单位(秒)</param>
    public void SpinToIndex(int targetIndex, int loops, float tweenTime)
    {
        // 确保循环模式开启
        scroller.Loop = true;

        // 计算要跳转的「远端」索引
        // realTargetIndex：真正想落到的下标
        // loops：想多滚几圈
        // scroller.NumberOfCells：总条目数
        int bigIndex = targetIndex + scroller.NumberOfCells * loops;

        // 调用 JumpToDataIndex 做一次性补间动画
        scroller.JumpToDataIndex(
            bigIndex,
            scrollerOffset: 0.5f,     // 让目标 cell 最终位于可视区域中间 (可根据需求调整)
            cellOffset: 0.5f,        // 单元格也居中
            useSpacing: true,
            tweenType: scroller.snapTweenType,  // 也可自定义 EaseInOut、Bounce 等
            tweenTime: tweenTime,               // 动画时长
            jumpComplete: () =>
            {
                Debug.Log($"完成单次补间动画，多圈后停在下标 {targetIndex}");
            }
        );
    }
    //异步版本,可等待
    public async UniTask SpinToIndexAsync(int targetIndex, int loops, float tweenTime)
    {
        // 确保循环模式开启
        scroller.Loop = true;

        // 计算「远端」索引
        int bigIndex = targetIndex + scroller.NumberOfCells * loops;

        // 创建完成通知
        var completionSource = new UniTaskCompletionSource();
        
        // 启动补间动画
        scroller.JumpToDataIndex(
            bigIndex,
            scrollerOffset: 0.5f,  // 目标 cell 居中
            cellOffset: 0.5f,
            useSpacing: true,
            tweenType: scroller.snapTweenType,  // 补间类型
            tweenTime: tweenTime,
            jumpComplete: () =>
            {
                Debug.Log($"完成补间动画，停在下标 {targetIndex}");
                completionSource.TrySetResult();
            }
        );
        
        // 等待补间完成
        await completionSource.Task;
    }

    /// <summary>返回数据总数</summary>
    public int GetNumberOfCells(EnhancedScroller scroller) => dataList.Count;

    /// <summary>
    /// 返回单元格高度（如果数据没给高度，则使用预制体高度）
    /// </summary>
    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        // 对 dataIndex 进行取模，确保在范围内
        int index = dataList.Count > 0 ? dataIndex % dataList.Count : 0;
        var data = dataList[index];

        // 1. 先尝试数据类的 CalculateHeight()
        float size = data.CalculateSize();
        if (size > 0) return size;
        
        // 2. 如果未指定，则从 prefab 获取默认高度
        var prefab = GetScrollerView(data);
        if (prefab != null)
        {
            Rect rect = prefab.GetComponent<RectTransform>().rect;
            return orientation == EnhancedScroller.ScrollDirectionEnum.Horizontal ? rect.width : rect.height;
        }

        // 3. 兜底默认 100f 避免异常
        return 100f;
    }

    /// <summary>返回单元格视图</summary>
    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        // 对 dataIndex 做模运算
        int index = dataList.Count > 0 ? dataIndex % dataList.Count : 0;
        BaseCellData data = dataList[index];

        // 根据数据类型名称，获取相应的预制体
        var prefab = GetScrollerView(data);
        if (prefab == null)
        {
            Debug.LogError($"未注册 {data.GetType().Name} 对应的预制体！");
            return null;
        }

        // 获取单元格对象
        EnhancedScrollerCellView cellView = scroller.GetCellView(prefab);
        cellView.SetData(data);
        return cellView;
    }
    
    /// <summary>
    /// 更新数据,保持当前滑动进度
    /// </summary>
    public void RefreshDataPreservePosition()
    {
        scroller.ReloadData(scroller.NormalizedScrollPosition);
    }
    
    /// <summary>
    /// 获取数据对应的视图类
    /// 目前的约束是数据拟定为 XXXCellData 然后它对应的视图就是 XXXCellView
    /// </summary>
    private EnhancedScrollerCellView GetScrollerView(BaseCellData data) 
    {
        string viewName = data.GetType().Name.Replace("CellData", "CellView");
        return ScrollerCellViews.Find(x => x.GetType().Name == viewName);
    }
}
