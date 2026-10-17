using UnityEngine;

public interface IUIManager
{
    public void Init(Camera uiCamera);

    public Cysharp.Threading.Tasks.UniTask<T> OpenPanelAsync<T>(BasePanelArg panelArg = null) where T : BasePanel, new();
    public T OpenPanel<T>(BasePanelArg arg = null) where T : BasePanel, new();

    public void ClosePanel<T>() where T : BasePanel, new();
}

