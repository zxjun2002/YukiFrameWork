using UnityEngine;

public abstract class BasePanel: MonoBehaviour
{
    //子类用于重写的初始化函数,abstract代表必须重写
    public virtual void Init(){}
    //子类用于重写的显示时函数
    public virtual void OnShow(BasePanelArg arg = null) { }
    //子类用于重写的关闭时函数
    public virtual void OnClose() { }

    #region 业务代码
    public void OpenPanel()
    {
        gameObject.SetActive(true);
    }

    public void SetPanelOrder(int sortinglayer)
    {
        var a =transform.Find("Canvas");
        transform.Find("Canvas").GetComponent<Canvas>().sortingOrder = sortinglayer;
    }
    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
    #endregion
}

public interface BasePanelArg //用于实现打开界面时一些自定义参数
{

}