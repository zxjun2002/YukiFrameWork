using UnityEngine;

public interface IUIManager
{
    public void Init(Camera uiCamera);
    public T OpenPanel<T>(BasePanelArg arg = null) where T : BasePanel, new();

    public void ClosePanel<T>() where T : BasePanel, new();
}

