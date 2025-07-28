using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Yuki;

namespace MCRogue
{
    public partial class HttpModule : IHttpModule
    {
        // 并发控制标志
        private bool isRequestInProgress = false;
        public bool GetisRequestInProgress() => isRequestInProgress;

        // 服务器时间同步
        private long _baseServerTimeAt;
        private readonly Stopwatch _serverTimeTimer = new Stopwatch();
        public long GetServerTimeAt() => _baseServerTimeAt + _serverTimeTimer.ElapsedMilliseconds;

        // PUT 请求（带重试）
        public async UniTask<T> Request_Put<T>(int gameReqType, string json) where T : List<GameBase_Rsp>
        {
            var requestUrl = $"{Url}/game";
            if (isRequestInProgress)
            {
                GameLogger.LogWarning("[Net][Rsp]: 上一个请求尚未完成，请稍后再试。");
                return default;
            }
            isRequestInProgress = true;
            try
            {
                var headers = CreateGameHeaders(gameReqType);
                GameLogger.LogWithColor($"[Net][Req][Put]: {gameReqType} => Url:{requestUrl}, Json:{json}", "#ff0000");
                return await RequestSendWithRetry<T>(() => CreateRequest(requestUrl, HTTPVerbs.Put, json, headers), json);
            }
            finally
            {
                isRequestInProgress = false;
            }
        }

        // PUT 请求（无重试）
        public async UniTask<T> Request_Put_Async<T>(int gameReqType, string json) where T : List<GameBase_Rsp>
        {
            var requestUrl = $"{Url}/game";
            var headers = CreateGameHeaders(gameReqType);
            using var request = CreateRequest(requestUrl, HTTPVerbs.Put, json, headers);
            GameLogger.LogWithColor($"[Net][Req][Put Async]: Url:{requestUrl}, Json:{json}", "#ff0000");
            return await Async_RequestSend<T>(request, json, headers);
        }

        // POST 请求（带重试）
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
                var requestUrl = $"{Url}{path}";
                GameLogger.LogWithColor($"[Net][Req][Post]: Url:{requestUrl}, Json:{json}", "#ff0000");
                return await RequestSendWithRetry<T>(() => CreateRequest(requestUrl, HTTPVerbs.Post, json, headers), json);
            }
            finally
            {
                isRequestInProgress = false;
            }
        }

        // POST 请求（无重试）
        public async UniTask<T> Request_Post_Async<T>(string path, string json, Dictionary<string, string> headers = null)
        {
            var requestUrl = $"{Url}{path}";
            using var request = CreateRequest(requestUrl, HTTPVerbs.Post, json, headers);
            GameLogger.LogWithColor($"[Net][Req][Post Async]: Url:{requestUrl}, Json:{json}", "#ff0000");
            return await Async_RequestSend<T>(request, json, headers);
        }

        // 构建 UnityWebRequest
        private UnityWebRequest CreateRequest(string url, HTTPVerbs verb, string json, Dictionary<string, string> headers)
        {
            var req = new UnityWebRequest(url, verb.ToString())
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Content-Type", "application/json");
            if (headers != null)
            {
                foreach (var kv in headers)
                    req.SetRequestHeader(kv.Key, kv.Value);
            }
            return req;
        }

        // 构建游戏业务请求头
        private Dictionary<string, string> CreateGameHeaders(int gameReqType)
            => new Dictionary<string, string>
            {
                ["Service-Type"] = "game",
                ["Content-Type"] = "application/json",
                ["Game-Server-Id"] = "1141514",
                ["Session-Id"] = "a",
                ["Session-Token"] = "b",
                ["Game-Api-Id"] = gameReqType.ToString()
            };

        // 带重试发送
        private async UniTask<T> RequestSendWithRetry<T>(Func<UnityWebRequest> factory, string json,
            int maxRetries = HttpConfig.DefaultMaxRetries,
            int delayMs = HttpConfig.DefaultRetryInterval,
            int timeoutMs = HttpConfig.RequestTimeout)
        {
            for (int i = 1; i <= maxRetries; i++)
            {
                using var req = factory();
                var rsp = await RequestSend<T>(req, json, timeoutMs);
                if (EqualityComparer<T>.Default.Equals(rsp, default))
                {
                    GameLogger.LogWarning($"[Net][Retry][{i}]: 请求失败，重试中...");
                    await UniTask.Delay(delayMs);
                    continue;
                }
                return rsp;
            }
            // 重试失败
            GameLogger.LogError("[Net]: 重试失败，触发登录");
            throw new Exception("[Net] 请求失败");
        }

        // 发送并处理响应
        private async UniTask<T> RequestSend<T>(UnityWebRequest webRequest, string json, int timeoutMs)
        {
            var op = webRequest.SendWebRequest();
            using var cts = new CancellationTokenSource(timeoutMs);
            try
            {
                await op.WithCancellation(cts.Token);
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    UpdateServerTime(webRequest);
                    GameLogger.LogWithColor($"[Net][Rsp]: {webRequest.downloadHandler.text}", "#ff0000");
                    return JsonConvert.DeserializeObject<T>(webRequest.downloadHandler.text);
                }
                throw new Exception(webRequest.error);
            }
            catch (OperationCanceledException)
            {
                GameLogger.LogError($"[Net][Rsp]: 请求超时 Json:{json}");
                return default;
            }
            catch (UnityWebRequestException)
            {
                return HandleWebError<T>(webRequest, json);
            }
            catch (Exception ex)
            {
                GameLogger.LogError($"[Net][Rsp]: 意外错误 Json:{json}, Err:{ex.Message}");
                return default;
            }
        }

        // 异步版发送（仅处理 Kick 业务码）
        private async UniTask<T> Async_RequestSend<T>(UnityWebRequest webRequest, string json, Dictionary<string, string> headers = null)
        {
            // 初始化上传/下载和头
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            if (headers != null)
            {
                foreach (var kv in headers)
                    webRequest.SetRequestHeader(kv.Key, kv.Value);
            }

            // 发送请求并超时控制
            var op = webRequest.SendWebRequest();
            using (var cts = new CancellationTokenSource(HttpConfig.RequestTimeout))
            {
                try
                {
                    await op.WithCancellation(cts.Token);
                }
                catch
                {
                    // 超时或异常，静默处理
                    GameLogger.LogError($"[Net][Rsp][Async]: 请求异常 Json:{json}");
                    return default;
                }
            }

            // 成功响应
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                // 更新服务器时间基准
                UpdateServerTime(webRequest);
                GameLogger.LogWithColor($"[Net][Rsp][Async]: {webRequest.downloadHandler.text}", "#ff0000");
                return JsonConvert.DeserializeObject<T>(webRequest.downloadHandler.text);
            }

            // 非200响应，仅在 Kick 场景下处理
            if ((HttpCode)webRequest.responseCode == HttpCode.kick)
            {
                var info = JsonConvert.DeserializeObject<HttpInfo>(webRequest.downloadHandler.text);
                throw new Exception($"[Net][Rsp]: 用户被踢出游戏, RspText:{webRequest.downloadHandler.text}");
            }

            // 其他情况静默返回
            return default;
        }

        // 更新服务器时间基准
        private void UpdateServerTime(UnityWebRequest webRequest)
        {
            string ts = webRequest.GetResponseHeader("Now-Timestamp");
            if (!string.IsNullOrEmpty(ts) && long.TryParse(ts, out var serverTs))
            {
                _baseServerTimeAt = serverTs;
                _serverTimeTimer.Restart();
            }
        }

        // 业务错误处理
        private T HandleWebError<T>(UnityWebRequest webRequest, string json)
        {
            var httpCode = (HttpCode)webRequest.responseCode;
            var rspText = webRequest.downloadHandler.text;
            var info = JsonConvert.DeserializeObject<HttpInfo>(rspText);
            switch (httpCode)
            {
                case HttpCode.kick:
                    throw new Exception($"[Net][Rsp]: 用户被踢出游戏,RspText:{rspText}");
                case HttpCode.error:
                    GameLogger.LogError($"[Net][Rsp]: 请求失败, Json:{json}, RspText:{rspText}");
                    throw new Exception("[Net][Rsp]: 请求失败");
                case HttpCode.update:
                    GameLogger.LogError($"[Net][Rsp]: 请求失败, Json:{json}, RspText:{rspText}");
                    throw new Exception("[Net][Rsp]: 请求失败");
                default:
                    GameLogger.LogError($"[Net][Rsp]: 其他错误, Json:{json}, RspText:{rspText}");
                    return default;
            }
        }
    }
}
