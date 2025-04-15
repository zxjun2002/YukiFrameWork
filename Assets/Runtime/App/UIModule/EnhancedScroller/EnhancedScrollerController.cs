using System;
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
    /// 异步补间方法：滚动到目标下标，并返回任务以等待动画完成
    /// </summary>
    public async UniTask SpinToIndexAsync(
        int   targetIndex,   // 想停的条目下标（0‑based）
        int   loops,         // 总圈数
        float totalTime)     // 整段动画时长（秒）
    {
        scroller.Loop = true;           // 无限循环模式
        scroller.snapping = false;      // 关掉 Snap，免得中途误触

        int   cells           = scroller.NumberOfCells;
        float oneLoopDistance = scroller.GetScrollPositionForCellViewIndex(
            cells, EnhancedScroller.CellViewPositionEnum.Before);

        /* ——————————————————————
           ① 先“自由旋转” loops‑1 圈
           —————————————————————— */
        int   freeLoops = Mathf.Max(loops - 1, 0);
        float freeTime  = totalTime * 0.75f;          // 前 75 % 时间高速旋转
        float stopTime  = totalTime - freeTime;       // 后 25 % 用补间精准收尾

        if (freeLoops > 0)
        {
            // 用恒定速度跑 freeLoops 圈
            float speed =  (oneLoopDistance * freeLoops) / freeTime; // 像素/秒
            // 方向：垂直向下用负值，横向向右用正值，可按需要改符号
            scroller.LinearVelocity = -speed;

            await UniTask.Delay(TimeSpan.FromSeconds(freeTime));
            scroller.LinearVelocity = 0;              // 刹车
        }

        /* ——————————————————————
           ② 计算“最近的同内容副本”
           —————————————————————— */
        int currentCell = scroller.GetCellViewIndexAtPosition(scroller.ScrollPosition);
        int forwardDelta = (targetIndex - (currentCell % cells) + cells) % cells;
        int finalCell    = currentCell + forwardDelta;        // 永远 ≤ 1 圈

        /* ——————————————————————
           ③ 补间到 finalCell，精确落点
           —————————————————————— */
        var tcs = new UniTaskCompletionSource();
        scroller.JumpToDataIndex(
            finalCell,
            scrollerOffset : 0.5f,
            cellOffset     : 0.5f,
            useSpacing     : true,
            tweenType      : scroller.snapTweenType,
            tweenTime      : stopTime,
            jumpComplete   : () => tcs.TrySetResult(),
            loopJumpDirection : EnhancedScroller.LoopJumpDirectionEnum.Closest
        );
        await tcs.Task;

        scroller.snapping = true;       // 如有需要再打开
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
