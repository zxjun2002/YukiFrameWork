using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;

namespace Yuki
{
    public partial class HttpModule
    {
        bool isRequestInProgress = false; // 请求进行中的标志
        string Req_Color = "#F3D600"; //同步
        string AsyncReq_Color = "#988710";
        string Rsp_Color = "#F47800";
        string AsyncRsp_Color = "#7D3E00";
        public bool GetisRequestInProgress() => isRequestInProgress;

        public async UniTask<T> Request_Put<T>(int gameReqType, string json) where T : List<GameBase_Rsp>
        {
            var game_url = $"{Url}/game";
            if (isRequestInProgress)
            {
                GameLogger.LogWarning("[Net][Rsp]: 上一个请求尚未完成，请稍后再试。");
                return default;
            }

            isRequestInProgress = true;
            try
            {
                Dictionary<string, string> Game_Headers = new Dictionary<string, string>();
                Game_Headers.Add("Game-Api-Id", gameReqType.ToString());
                Func<UnityWebRequest> createWebRequest = () =>
                {
                    var webRequest = new UnityWebRequest(game_url, HTTPVerbs.Put.ToString());
                    webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    foreach (var header in Game_Headers)
                    {
                        webRequest.SetRequestHeader(header.Key, header.Value);
                    }

                    return webRequest;
                };

                GameLogger.LogWithColor(
                    $"[Net][Req][Put]: {gameReqType} => 【Url】: {game_url}, 【Json】: {json}, 【Headers】:{string.Join(", ", Game_Headers.Select(kv => $"[{kv.Key}: {kv.Value}]"))}",
                    AsyncReq_Color);
                return await RequestSendWithRetry<T>(createWebRequest);
            }
            finally
            {
                isRequestInProgress = false; // 请求完成后重置标志
            }
        }

        public async UniTask<T> Request_Put_Async<T>(int gameReqType, string json) where T : List<GameBase_Rsp>
        {
            var game_url = $"{Url}/game";
            Dictionary<string, string> Game_Headers = new Dictionary<string, string>();
            Game_Headers.Add("Game-Api-Id", gameReqType.ToString());
            using (var webRequest = new UnityWebRequest(game_url, HTTPVerbs.Put.ToString()))
            {
                GameLogger.LogWithColor(
                    $"[Net][Req][Get]: =>【Url】:{game_url},【Json】:{json},【Headers】:{string.Join(", ", Game_Headers.Select(kv => $"[{kv.Key}: {kv.Value}]"))}",
                    Req_Color);
                return await Async_RequestSend<T>(webRequest, json, Game_Headers);
            }
        }

        public async UniTask<T> Request_Post<T>(string path, string json, Dictionary<string, string> headers = null)
        {
            if (isRequestInProgress)
            {
                GameLogger.LogWarning("[Net][Rsp]: 上一个请求尚未完成，请稍后再试。");
                return default;
            }

            isRequestInProgress = true;
            try
            {
                Func<UnityWebRequest> createWebRequest = () =>
                {
                    var webRequest = new UnityWebRequest($"{Url}{path}", HTTPVerbs.Post.ToString());
                    webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    webRequest.downloadHandler = new DownloadHandlerBuffer();

                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            webRequest.SetRequestHeader(header.Key, header.Value);
                        }
                    }

                    return webRequest;
                };

                GameLogger.LogWithColor(
                    $"[Net][Req][Post]: 【URL】: {Url}{path}, 【JSON】: {json}, 【Headers】:{string.Join(", ", headers.Select(kv => $"[{kv.Key}: {kv.Value}]"))}",
                    Req_Color);
                return await RequestSendWithRetry<T>(createWebRequest);
            }
            finally
            {
                isRequestInProgress = false; // 请求完成后重置标志
            }
        }

        public async UniTask<T> Request_Post_Async<T>(string path, string json,
            Dictionary<string, string> headers = null)
        {
            using (var webRequest = new UnityWebRequest($"{Url}{path}", HTTPVerbs.Post.ToString()))
            {
                GameLogger.LogWithColor($"[Net][Req][Get]: =>Url:{Url}{path},Json:{json}", Req_Color);
                return await Async_RequestSend<T>(webRequest, json, headers);
            }
        }

        private async UniTask<T> RequestSendWithRetry<T>(Func<UnityWebRequest> requestFactory,
            int maxRetries = HttpConfig.DefaultMaxRetries, int delayMilliseconds = HttpConfig.DefaultRetryInterval,
            int timeoutMilliseconds = HttpConfig.RequestTimeout)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                UnityWebRequest webRequest = null;
                try
                {
                    webRequest = requestFactory();

                    // 调用带超时控制的请求发送逻辑
                    var response = await RequestSend<T>(webRequest, timeoutMilliseconds);

                    // 如果返回值是默认值，则认为请求失败，进行重试
                    if (EqualityComparer<T>.Default.Equals(response, default))
                    {
                        GameLogger.LogWarning($"[Net][Retry][Attempt {attempt}]: 请求失败，等待重试...");
                        await UniTask.Delay(delayMilliseconds); // 重试前等待
                        continue; // 重试
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    GameLogger.LogError($"[Net][Retry][Attempt {attempt}]: 出现意外错误 {ex.Message}");
                    throw;
                }
                finally
                {
                    webRequest?.Dispose(); // 手动释放资源
                }

            }

            // 最后一次重试失败后，触发登录弹窗
            GameLogger.LogError("[Net]: 所有重试均失败，触发重新登录逻辑");

            throw new Exception("[Net] 请求失败，所有重试均未成功");
        }

        private async UniTask<T> RequestSend<T>(UnityWebRequest webRequest,
            int timeoutMilliseconds = HttpConfig.RequestTimeout)
        {
            //IgnoreVerification_SSL(webRequest); // 忽略 SSL 验证（如果需要）

            var operation = webRequest.SendWebRequest();
            using (var cancellationTokenSource = new CancellationTokenSource(timeoutMilliseconds))
            {
                try
                {
                    // 设置超时控制
                    await operation.WithCancellation(cancellationTokenSource.Token);

                    // 请求成功时解析并返回数据
                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        GameLogger.LogWithColor($"[Net][Rsp]: {webRequest.downloadHandler.text}", Rsp_Color);
                        return JsonConvert.DeserializeObject<T>(webRequest.downloadHandler.text);
                    }

                    GameLogger.LogError("[Net][Rsp]: 不应该在这里");
                    throw new Exception(webRequest.error);
                }
                catch (OperationCanceledException)
                {
                    GameLogger.LogError("[Net][Rsp]: 请求超时");
                    return default;
                }
                catch (UnityWebRequestException ex)
                {
                    // 如果是踢出游戏的异常，不需要返回默认值，直接抛出即可
                    // 请求失败时处理错误码
                    var httpCode = (HttpCode)webRequest.responseCode;

                    // 处理特定的业务错误，比如踢出游戏
                    if (httpCode == HttpCode.kick)
                    {
                        throw new Exception(
                            $"[Net][Rsp]: 用户被踢出游戏,RspText:{webRequest.downloadHandler.text}"); // 抛出异常，终止请求
                    }

                    if (httpCode == HttpCode.error)
                    {
                        GameLogger.LogError($"[Net][Rsp]: 请求失败,RspText:{webRequest.downloadHandler.text}");
                        throw new Exception($"[Net][Rsp]: 请求失败"); // 抛出异常，终止请求
                    }

                    if (httpCode == HttpCode.update)
                    {
                        GameLogger.LogError($"[Net][Rsp]: 请求失败,RspText:{webRequest.downloadHandler.text}");
                        throw new Exception($"[Net][Rsp]: 请求失败"); // 抛出异常，终止请求
                    }

                    // 如果是其他的错误，抛出异常
                    GameLogger.LogError(
                        $"[Net][Rsp]: 其他错误 RspText:{webRequest.downloadHandler.text}，Message:{ex.Message}");
                    return default;
                    ;
                }
                catch (Exception ex)
                {
                    // 捕获其他异常并记录
                    GameLogger.LogError($"[Net][Rsp]: 出现意外错误：{ex.Message}");
                    return default;
                }
            }
        }

        private async UniTask<T> Async_RequestSend<T>(UnityWebRequest webRequest, string json,
            Dictionary<string, string> headers = null)
        {
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            if (headers != null)
                foreach (var header in headers)
                    webRequest.SetRequestHeader(header.Key, header.Value);


            //IgnoreVerification_SSL(webRequest);
            var operation = webRequest.SendWebRequest();

            // 使用 CancellationToken 进行超时处理
            using (var cancellationTokenSource = new CancellationTokenSource(HttpConfig.RequestTimeout))
            {
                try
                {
                    await operation.WithCancellation(cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    GameLogger.LogError("[Net][Rsp][Get]:请求超时");
                    return default;
                }
                catch (UnityWebRequestException ex) // 捕获 UnityWebRequest 异常
                {
                    GameLogger.LogError($"[Net][Rsp]: 请求失败，错误信息：{ex.Message}");
                    return default;
                }
                catch (Exception ex) // 捕获其他异常
                {
                    GameLogger.LogError($"[Net][Rsp]: 出现意外错误：{ex.Message}");
                    return default;
                }
            }

            GameLogger.LogWithColor("[Net][Rsp][Get]:" + webRequest.downloadHandler.text, AsyncRsp_Color);
            return JsonConvert.DeserializeObject<T>(webRequest.downloadHandler.text);
        }
    }

    /// <summary>
    /// 允许忽略 SSL 验证
    /// </summary>
    public class WebReqSkipCert : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}
