using System;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MIKUFramework.IOC;
using UnityEngine.Events;
using UnityEngine.UI;

public abstract class UIListItemData
{
    public int Index;
}

public abstract class UIListItem : MonoBehaviour
{
    public Action<UIListItemData> showData { get; private set; }
    public Action onShow { get; private set; }
    public Action onHide { get; private set; }
    public Action init { get; private set; }
    
    private bool hasShown = false;

    public virtual void AddInit()
    {
        init ??= Init;
    }

    public void ResetShowState()
    {
        hasShown = false;
    }

    public void TryInvokeOnShow()
    {
        if (!hasShown)
        {
            onShow?.Invoke();
            hasShown = true;
        }
    }

    public virtual void AddShowData()
    {
        showData ??= ShowData;
    }

    public virtual void AddOnShow()
    {
        onShow ??= OnShow;
    }

    public virtual void AddOnHide()
    {
        onHide ??= OnHide;
    }

    //Init => OnShow => ShowData=> OnHide
    private bool isInjected = false;
    protected virtual void Init()
    {
        if (isInjected) return;
        IoCHelper.Instance.Inject(this);
        isInjected = true;
    }
    protected virtual void OnShow() { }
    protected abstract void ShowData(UIListItemData baseData);
    protected virtual void OnHide() { }
}

[RequireComponent(typeof(UnityEngine.UI.LoopScrollRect))]
[DisallowMultipleComponent]
public class UIList : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
{
    [SerializeField] GameObject item;

    Stack<Transform> pool = new Stack<Transform>();
    private LoopScrollRect ls;

    #region 业务调用

    public UnityEvent<Vector2> OnScrollValueChanged;

    public Func<int, UIListItemData> SetIndexData;

    public void SetCount(int count)
    {
        if (ls == null)
        {
            ls = GetComponent<LoopScrollRect>();
            ls.onValueChanged.RemoveAllListeners();
            ls.onValueChanged.AddListener(HandleScrollValueChanged);
        }
        ls.prefabSource = this;
        ls.dataSource = this;
        ls.totalCount = count;
        ls.RefillCells();
    }

    public void UpdateContent()
    {
        ls.RefreshCells();
    }

    public void ScrollToCellWithinTime(int index, float time = 0.5f)
    {
        ls.ScrollToCellWithinTime(index, time);
    }

    public async UniTask ScrollToCellWithinTimeAsync(int index, float time = 0.5f, CancellationToken cts = default)
    {
        await ls.ScrollToCellWithinTimeAsync(index, time, cts);
    }

    public Vector3 GetItemWorldPosition(int index)
    {
        RectTransform contentRect = ls.content;
        RectTransform targetItem = contentRect.GetChild(index).GetComponent<RectTransform>();
        return targetItem.position;
    }

    #endregion

    #region 组件接口

    public GameObject GetObject(int index)
    {
        UIListItem listItem;
        if (pool.Count == 0)
        {
            var instance = Instantiate(item);
            listItem = instance.GetComponent<UIListItem>();
        }
        else
        {
            var pooledTransform = pool.Pop();
            pooledTransform.gameObject.SetActive(true);
            listItem = pooledTransform.GetComponent<UIListItem>();
        }

        if (listItem != null)
        {
            listItem.AddInit();
            listItem.init?.Invoke();
            listItem.AddOnShow();
            //listItem.onShow?.Invoke();
        }

        return listItem.gameObject;
    }

    public void ReturnObject(Transform trans)
    {
        var listItem = trans.GetComponent<UIListItem>();
        if (listItem != null)
        {
            listItem.onHide?.Invoke();
            listItem.ResetShowState();
        }

        trans.gameObject.SetActive(false);
        trans.SetParent(transform, false);
        pool.Push(trans);
    }

    public void ProvideData(Transform transform, int idx)
    {
        var listItem = transform.GetComponent<UIListItem>();
        if (listItem != null)
        {
            listItem.AddOnShow();
            listItem.AddShowData();
            listItem.AddOnHide();


            var itemData = SetIndexData?.Invoke(idx);
            if (itemData != null)
            {
                listItem.showData.Invoke(itemData);
                listItem.TryInvokeOnShow();
            }
            else
            {
                GameLogger.LogError($"[UIList] 提供的数据为空 idx={idx}");
            }
        }
        else
        {
            GameLogger.LogError($"[UIList]找不到UIListItem！！！idx={idx}");
        }
    }

    #endregion

    #region 生命周期

    private void OnDestroy()
    {
        if (ls != null)
        {
            ls.onValueChanged.RemoveAllListeners();
        }
    }

    private void HandleScrollValueChanged(Vector2 position)
    {
        OnScrollValueChanged?.Invoke(position);
    }

    #endregion
}