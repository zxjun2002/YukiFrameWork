public interface IConfigTable 
{
    public void Init(string url);

    public T GetConfig<T>() where T : class, IRacastSet;
}