using EnhancedUI.EnhancedScroller;
using TMPro;

/// <summary>物品数据类型</summary>
public class ItemCellData : BaseCellData 
{
    public string Index;
}

public class ItemCellView : EnhancedScrollerCellView
{
    public TextMeshProUGUI IndexText;

    public override void SetData(BaseCellData data)
    {
        if (data is ItemCellData headerData)
        {
            IndexText.text = headerData.Index;
        }
    }
}