using System;
using System.Collections.Generic;
using EnhancedUI.EnhancedScroller;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>普通内容项数据类型</summary>
public class ContentBaseCellData : BaseCellData 
{
    public List<int> dataIndexList;
    public ObjectPool<GameObject> pool;
    
    public override float CalculateHeight()
    {
        return (float) Math.Ceiling(dataIndexList.Count / 3.0f) * 100f;
    }
}

public class ContentCellView : EnhancedScrollerCellView
{
    public Transform parentTransform;

    public override void SetData(BaseCellData data)
    {
        if (data is ContentBaseCellData headerData)
        {
            foreach (var Index in headerData.dataIndexList)
            {
                var obj = headerData.pool.Get();
                obj.transform.SetParent(parentTransform);
                obj.transform.localPosition = Vector3.one;
                obj.transform.Find("Info").GetComponent<TextMeshProUGUI>().text = Index.ToString();
            }
        }
    }
}