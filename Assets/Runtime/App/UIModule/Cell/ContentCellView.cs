using System.Collections.Generic;
using EnhancedUI.EnhancedScroller;

/// <summary>普通内容项数据类型</summary>
public class ContentCellData : BaseCellData
{
    public List<int> dataIndexList;
}

public class ContentCellView : EnhancedScrollerCellView
{
    public EnhancedScrollerController controller;

    public override void SetData(BaseCellData data)
    {
        if (data is ContentCellData headerData)
        {
            List<BaseCellData> itemData = new List<BaseCellData>();
            foreach (var idx in headerData.dataIndexList)
            {
                itemData.Add(new ItemCellData()
                {
                    Index = idx.ToString()
                });
            }
            controller.SetData(itemData);
        }
    }
}