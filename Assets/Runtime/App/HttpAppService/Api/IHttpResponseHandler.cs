namespace Yuki
{
    public interface IResponseHandler<T>
    {
        void HandleResponse(T responseData);
    }
}