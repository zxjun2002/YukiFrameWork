using System;
using System.Collections.Generic;
using UnityEngine;
using EnhancedUI;
using EnhancedUI.EnhancedScroller;

/// <summary>
/// EnhancedScroller 的通用封装，适用于任何类型的 BaseCellData。
/// 不再直接存储 HeaderPrefab、ItemPrefab，而是外部注册类型 -> 预制体的映射。
/// </summary>
public class EnhancedScrollerController : MonoBehaviour, IEnhancedScrollerDelegate
{
    private EnhancedScroller scroller;

    // 维护数据
    private List<BaseCellData> dataList = new List<BaseCellData>();

    // 维护 数据类型 -> 预制体 映射关系（外部注入）
    private Dictionary<Type, EnhancedScrollerCellView> prefabMapping = new Dictionary<Type, EnhancedScrollerCellView>();
    
    public void Awake()
    {
        scroller = gameObject.GetComponent<EnhancedScroller>();
    }

    /// <summary>
    /// 外部注册数据类型对应的预制体
    /// </summary>
    public void RegisterPrefab<T>(EnhancedScrollerCellView prefab) where T : BaseCellData
    {
        prefabMapping[typeof(T)] = prefab;
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

    /// <summary>返回数据总数</summary>
    public int GetNumberOfCells(EnhancedScroller scroller) => dataList.Count;

    /// <summary>
    /// 返回单元格高度（如果数据没给高度，则使用预制体高度）
    /// </summary>
    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        var data = dataList[dataIndex];

        // 1. 先尝试数据类的 CalculateHeight()
        float height = data.CalculateHeight();
        if (height > 0) return height;

        // 2. 如果未指定，则从注册的 prefab 获取默认高度
        if (prefabMapping.TryGetValue(data.GetType(), out EnhancedScrollerCellView prefab) && prefab != null)
        {
            return prefab.GetComponent<RectTransform>().rect.height;
        }

        // 3. 兜底默认 100f 避免异常
        return 100f;
    }

    /// <summary>返回单元格视图</summary>
    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        BaseCellData data = dataList[dataIndex];

        // 根据数据类型，获取相应的预制体
        if (!prefabMapping.TryGetValue(data.GetType(), out EnhancedScrollerCellView prefab))
        {
            Debug.LogError($"未注册 {data.GetType().Name} 对应的预制体！");
            return null;
        }

        // 获取单元格对象
        EnhancedScrollerCellView cellView = scroller.GetCellView(prefab);
        cellView.SetData(data);
        return cellView;
    }
}
