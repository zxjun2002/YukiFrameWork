using System;
using System.Collections.Generic;
using MIKUFramework.IOC;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[Component]
public class UIManager:IUIManager, IDisposable
{
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _prefabHandles = new();
    private bool _disposed;
    private Camera _uiCamera;
    private Transform _uiRoot;
    // 已打开界面的缓存字典
    private Dictionary<string, BasePanel> _opelPanelDict = new Dictionary<string, BasePanel>();
    //计数器，用于设置界面层级
    private static int _sortinglayer = 0;
    //缓存字典，存储读取和生成后的界面
    private Dictionary<string, BasePanel> _buffPanelDict = new Dictionary<string, BasePanel>();

    public Transform UIRoot
    {
        get
        {
            if (_uiRoot == null)
            {
                if (GameObject.Find("Root"))
                {
                    _uiRoot = GameObject.Find("Root").transform;
                }
                else
                {
                    _uiRoot = new GameObject("Root").transform;
                    GameObject.DontDestroyOnLoad(_uiRoot);
                }
            };
            return _uiRoot;
        }
    }

    public void Init(Camera uiCamera)
    { 
        _uiCamera = uiCamera;
    }

    public T OpenPanel<T>(BasePanelArg panelArg = null) where T : BasePanel, new()
    {
        string panelName = typeof(T).Name;
        if (_opelPanelDict.TryGetValue(panelName, out BasePanel panel))
        {
            Debug.LogWarning("界面已打开: " + panelName);
            return panel as T;
        }
        if (!_buffPanelDict.TryGetValue(panelName, out panel))
        {
            throw new InvalidOperationException($"界面 {panelName} 尚未加载，请先调用 OpenPanelAsync。");
        }
        _opelPanelDict.Add(panelName, panel);
        panel.OnShow(panelArg);
        panel.SetPanelOrder(_sortinglayer);
        _sortinglayer++;
        panel.OpenPanel();
        return panel as T;
    }

    public async UniTask<T> OpenPanelAsync<T>(BasePanelArg panelArg = null) where T : BasePanel, new()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UIManager));
        string name = typeof(T).Name;
        if (!_buffPanelDict.ContainsKey(name))
        {
            if (!_prefabHandles.TryGetValue(name, out var handle))
            {
                handle = Addressables.LoadAssetAsync<GameObject>("Panel/" + name);
                _prefabHandles.Add(name, handle);
            }
            GameObject prefab;
            try { prefab = await handle; }
            catch
            {
                _prefabHandles.Remove(name);
                if (handle.IsValid()) Addressables.Release(handle);
                throw;
            }
            if (_disposed) throw new ObjectDisposedException(nameof(UIManager));
            // 并发打开同一个界面只创建一个实例。
            if (!_buffPanelDict.ContainsKey(name)) _buffPanelDict.Add(name, CreatePanel<T>(prefab));
        }
        return OpenPanel<T>(panelArg);
    }
    public void ClosePanel<T>() where T : BasePanel, new()
    {
        string panelName = typeof(T).Name;
        if (!_opelPanelDict.TryGetValue(panelName, out BasePanel panel))
        {
            throw new Exception("界面未打开" + panelName);
        }
        panel.OnClose();
        if (_opelPanelDict.ContainsKey(panelName))
        {
            _opelPanelDict.Remove(panelName);
        }
        panel.ClosePanel();
    }


    private BasePanel CreatePanel<T>(GameObject panelPrefab) where T : BasePanel, new()
    {
        GameObject panelObject = GameObject.Instantiate(panelPrefab, UIRoot, false);
        BasePanel panel = panelObject.GetComponent<T>();
        if (panel == null)
        {
            GameObject.Destroy(panelObject);
            throw new InvalidOperationException($"界面预制体缺少 {typeof(T).Name} 组件。");
        }
        panel.Init(_uiCamera);
        return panel;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var panel in _opelPanelDict.Values) if (panel != null) panel.OnClose();
        foreach (var panel in _buffPanelDict.Values) if (panel != null) GameObject.Destroy(panel.gameObject);
        foreach (var handle in _prefabHandles.Values) if (handle.IsValid()) Addressables.Release(handle);
        _opelPanelDict.Clear();
        _buffPanelDict.Clear();
        _prefabHandles.Clear();
    }
}
