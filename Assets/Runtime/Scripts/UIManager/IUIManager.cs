public interface IUIManager 
{
    public T OpenPanel<T>(BasePanelArg arg = null) where T : BasePanel, new();

    public void ClosePanel<T>() where T : BasePanel, new();
}

