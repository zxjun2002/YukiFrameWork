using EnhancedUI.EnhancedScroller;
using TMPro;

/// <summary>标题数据类型</summary>
public class HeaderBaseCellData : BaseCellData 
{
    public string title;
}

public class HeaderCellView : EnhancedScrollerCellView
{
    public TextMeshProUGUI titleText;

    public override void SetData(BaseCellData data)
    {
        if (data is HeaderBaseCellData headerData)
        {
            titleText.text = headerData.title;
        }
    }
}