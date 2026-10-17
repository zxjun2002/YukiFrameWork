public interface IConfigTable 
{
    public void Init(string url);

    public Cysharp.Threading.Tasks.UniTask InitAsync(string address);

    public T GetConfig<T>() where T : class, IRacastSet;
}
