using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

public abstract class UIListItemData
{
    public int Index;
}

public abstract class UIListItem : MonoBehaviour
{
    public Action<UIListItemData> showData { get; private set; }

    public virtual void AddShowData()
    {
        showData += ShowData;
    }

    protected abstract void ShowData(UIListItemData baseData);
}

[RequireComponent(typeof(UnityEngine.UI.LoopScrollRect))]
[DisallowMultipleComponent]
public class UIList : MonoBehaviour, LoopScrollPrefabSource, LoopScrollDataSource
{
    /// <summary>监听滚动事件</summary>
    public UnityEvent<Vector2> OnScrollValueChanged;

    public Func<int, UIListItemData> SetIndexData;
    [SerializeField] GameObject item;
    
    Stack<Transform> pool = new Stack<Transform>();
    private LoopScrollRect ls;

    public GameObject GetObject(int index)
    {
        if (pool.Count == 0)
        {
            return Instantiate(item);
        }
        Transform candidate = pool.Pop();
        candidate.gameObject.SetActive(true);
        return candidate.gameObject;
    }
    
    public void ReturnObject(Transform trans)
    {
        //trans.SendMessage("ScrollCellReturn", SendMessageOptions.DontRequireReceiver);
        trans.gameObject.SetActive(false);
        trans.SetParent(transform, false);
        pool.Push(trans);
    }

    public void ProvideData(Transform transform, int idx)
    {
        var item = transform.GetComponent<UIListItem>();
        if (item != null)
        {
            if (item.showData == null)
            {
                item.AddShowData();
            }
            item.showData.Invoke(SetIndexData?.Invoke(idx));
        }
        else
        {
            GameLogger.LogError($"[UIList]找不到UIListItem！！！idx={idx}");
        }
        //transform.SendMessage("ScrollCellIndex", ItemDatas[idx]);
    }

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
    
    public Vector3 GetItemWorldPosition(int index)
    {
        // 获取内容区域中的 RectTransform
        RectTransform contentRect = ls.content;

        // 获取目标项的 RectTransform（假设每个项的父物体是 content）
        RectTransform targetItem = contentRect.GetChild(index).GetComponent<RectTransform>();

        // 获取该项的世界坐标
        Vector3 worldPosition = targetItem.position;

        return worldPosition;
    }
    
    #region 生命周期
    private void OnDestroy()
    {
        // 移除事件监听
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