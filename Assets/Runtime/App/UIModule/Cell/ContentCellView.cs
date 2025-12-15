using System.Collections.Generic;
using EnhancedUI.EnhancedScroller;
using MIKUFramework.IOC;

/// <summary>普通内容项数据类型</summary>
public class ContentCellData : BaseCellData
{
    public List<int> dataIndexList;
}

public class ContentCellView : EnhancedScrollerCellView
{
    [Autowired] private IConfigTable configTable;
    
    public EnhancedScrollerController controller;

    public override void SetData(BaseCellData data)
    {
        if (data is ContentCellData headerData)
        {
            GameLogger.LogGreen(configTable.GetConfig<BuffRacastSet>().EffectCtCt[102].effectVal);
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

    protected override void OnShow()
    {
        base.OnShow();
        GameLogger.LogYellow("ContentCellView OnShow");
    }
    
    protected override void OnHide()
    {
        base.OnHide();
        GameLogger.LogYellow("ContentCellView OnHide");
    }
}