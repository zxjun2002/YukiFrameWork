using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager
{
    private static UIManager _instance;
    private Transform _uiRoot;
    // 已打开界面的缓存字典
    private Dictionary<string, BasePanel> _opelPanelDict;
    //计数器，用于设置界面层级
    private static int _sortinglayer = 0;
    //缓存字典，存储读取和生成后的界面
    private Dictionary<string, BasePanel> _buffPanelDict;

    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new UIManager();
            }
            return _instance;
        }
    }

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
    private UIManager()
    {
        InitDicts();
    }

    private void InitDicts()
    {
        _opelPanelDict = new Dictionary<string, BasePanel>();
        _buffPanelDict = new Dictionary<string, BasePanel>();
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
            panel = CreatePanel<T>();
            _buffPanelDict.Add(panelName, panel);
        }
        _opelPanelDict.Add(panelName, panel);
        panel.OnShow(panelArg);
        panel.SetPanelOrder(_sortinglayer);
        _sortinglayer++;
        panel.OpenPanel();
        return panel as T;
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


    private BasePanel CreatePanel<T>() where T : BasePanel, new()
    {
        string panelName = typeof(T).Name;
        string path = "Panel/" + panelName;
        GameObject panelPrefab = Resources.Load<GameObject>(path);
        GameObject panelObject = GameObject.Instantiate(panelPrefab, UIRoot, false);
        BasePanel panel = panelObject.GetComponent<BasePanel>();
        panel.Init();
        return panel;
    }
}