using EnhancedUI.EnhancedScroller;
using TMPro;

/// <summary>物品数据类型</summary>
public class ItemBaseCellViewData : BaseCellData 
{
    public string Index;
}

public class ItemCellView : EnhancedScrollerCellView
{
    public TextMeshProUGUI IndexText;

    public override void SetData(BaseCellData data)
    {
        if (data is ItemBaseCellViewData headerData)
        {
            IndexText.text = headerData.Index;
        }
    }
}