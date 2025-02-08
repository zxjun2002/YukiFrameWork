using Google.Protobuf;

namespace Yuki
{
    public interface IResponseHandler<T>
    {
        void HandleResponse(T responseData, JsonParser jsonParser);
    }
}