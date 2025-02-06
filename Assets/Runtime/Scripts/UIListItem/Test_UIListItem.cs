using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Test_UIListItemData : UIListItemData//每一个列表项的数据
{
    public int num;
}

public partial class Test_UIListItem//UI定义
{
    [SerializeField] private TextMeshProUGUI NumText;
}

public partial class Test_UIListItem : UIListItem //逻辑定义
{
    protected override void ShowData(UIListItemData baseData)
    {
        if (baseData is Test_UIListItemData data)
        {   
            NumText.text = data.num.ToString();
        }
    }
}
