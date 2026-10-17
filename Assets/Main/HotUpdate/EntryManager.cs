using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Yuki.HotUpdate
{
    public sealed class EntryManager : MonoBehaviour
    {
        [SerializeField] private HotUpdateSettings settings;
        private HotUpdateLoader _loader;
        private bool _running;
        private string _error;

        private void Awake() => DontDestroyOnLoad(gameObject);
        private void Start() => StartUpdate();

        public void StartUpdate()
        {
            if (_running || (_loader != null && _loader.RequiresRestart)) return;
            RunAsync().Forget();
        }

        private async UniTask RunAsync()
        {
            _running = true;
            _error = null;
            _loader = new HotUpdateLoader();
            try
            {
                await _loader.RunAsync(settings, this.GetCancellationTokenOnDestroy());
                Destroy(gameObject);
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                _error = exception.Message;
                Debug.LogException(exception);
            }
            finally { _running = false; }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width * .2f, Screen.height * .4f, Screen.width * .6f, 200), GUI.skin.box);
            GUILayout.Label(_loader?.Status ?? "准备启动");
            if (_running) GUILayout.Label($"{_loader.Progress:P0}");
            if (_error != null)
            {
                GUILayout.Label(_error);
                if (_loader.RequiresRestart) GUILayout.Label("启动中断，请重启游戏。");
                else if (GUILayout.Button("重试")) StartUpdate();
            }
            GUILayout.EndArea();
        }
    }
}
