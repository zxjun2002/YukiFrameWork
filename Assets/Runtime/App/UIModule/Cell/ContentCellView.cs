using System.Collections.Generic;
using EnhancedUI.EnhancedScroller;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>普通内容项数据类型</summary>
public class ContentBaseCellData : BaseCellData
{
    public List<int> dataIndexList;
    public ObjectPool<GameObject> pool;
}

public class ContentCellView : EnhancedScrollerCellView
{
    public EnhancedScrollerController controller;
    public EnhancedScrollerCellView itemCellViewPrefab;

    public override void SetData(BaseCellData data)
    {
        if (data is ContentBaseCellData headerData)
        {
            controller.RegisterPrefab<ItemBaseCellViewData>(itemCellViewPrefab);
            List<BaseCellData> itemData = new List<BaseCellData>();
            foreach (var idx in headerData.dataIndexList)
            {
                itemData.Add(new ItemBaseCellViewData()
                {
                    Index = idx.ToString()
                });
            }
            controller.SetData(itemData);
        }
    }
}