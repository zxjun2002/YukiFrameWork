using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(LoopScrollRectMulti))]
[DisallowMultipleComponent]
public class UIListMulti : MonoBehaviour, LoopScrollPrefabSource, LoopScrollMultiDataSource
{
    [Header("Prefab 模式设置")]
    public bool isUseMultiPrefabs = false;
    [Tooltip("单Prefab模式下使用")]
    public GameObject singlePrefab;
    [Tooltip("多Prefab模式下使用")]
    public List<UIListItem> multiPrefabs;

    // 内部引用 LoopScrollRectMulti 组件
    private LoopScrollRectMulti loopScrollRect;

    // 单Prefab模式的对象池
    private Stack<Transform> poolSingle = new Stack<Transform>();
    // 多Prefab模式的对象池(以类型名字为 key)
    private Dictionary<string, Stack<Transform>> poolMulti = new Dictionary<string, Stack<Transform>>();
    
    /// <summary>监听滚动事件</summary>
    public UnityEvent<Vector2> OnScrollValueChanged;

    // 委托：根据索引返回 UIListItemData
    public Func<int, UIListItemData> SetIndexData;

    #region LoopScrollPrefabSource 实现

    public GameObject GetObject(int index)
    {
        if (!isUseMultiPrefabs)
        {
            // 单Prefab模式：先从对象池中取
            if (poolSingle.Count == 0)
            {
                Transform candidate = Instantiate(singlePrefab).transform;
                return candidate.gameObject;
            }
            else
            {
                Transform candidate = poolSingle.Pop();
                candidate.gameObject.SetActive(true);
                return candidate.gameObject;
            }
        }
        else
        {
            UIListItemData data = SetIndexData?.Invoke(index);
            //给定约束,预制体的名字和Data的名字相同,比如我的数据叫做Test_UIListItemData 那么我的视图脚本就叫做Test_UIListItem
            string prefabname = data?.GetType().Name.Replace("Data", "");
            GameObject prefab = multiPrefabs.Find(x => x.GetType().Name == prefabname).gameObject;
            if (!poolMulti.TryGetValue(prefab.name, out Stack<Transform> stack))
            {
                stack = new Stack<Transform>();
                poolMulti[prefab.name] = stack;
            }
            if (stack.Count == 0)
            {
                Transform candidate = Instantiate(prefab).transform;
                candidate.name = prefab.name;
                return candidate.gameObject;
            }
            else
            {
                Transform candidate = stack.Pop();
                candidate.gameObject.SetActive(true);
                return candidate.gameObject;
            }
        }
    }

    public void ReturnObject(Transform trans)
    {
        trans.gameObject.SetActive(false);
        trans.SetParent(this.transform, false);
        if (!isUseMultiPrefabs)
        {
            poolSingle.Push(trans);
        }
        else
        {
            string prefabName = trans.name;
            if (!poolMulti.TryGetValue(prefabName, out Stack<Transform> stack))
            {
                stack = new Stack<Transform>();
                poolMulti[prefabName] = stack;
            }
            stack.Push(trans);
        }
    }

    #endregion

    #region LoopScrollMultiDataSource 实现

    public void ProvideData(Transform t, int idx)
    {
        var listItem = t.GetComponent<UIListItem>();
        if (listItem != null)
        {
            if (listItem.showData == null)
            {
                listItem.AddShowData();
            }
            UIListItemData data = SetIndexData?.Invoke(idx);
            listItem.showData.Invoke(data);
        }
        else
        {
            Debug.LogError($"[UIListMulti] 找不到 UIListItem 脚本，idx = {idx}");
        }
    }

    #endregion

    #region 对外接口

    /// <summary>
    /// 外部调用此接口设置总数量并生成列表
    /// </summary>
    public void SetCount(int newCount, bool refill = true)
    {
        // 在第一次调用 SetCount 时初始化 LoopScrollRectMulti
        if (loopScrollRect == null)
        {
            loopScrollRect = GetComponent<LoopScrollRectMulti>();
            loopScrollRect.onValueChanged.RemoveAllListeners();
            loopScrollRect.onValueChanged.AddListener(HandleScrollValueChanged);
        }
        loopScrollRect.prefabSource = this;
        loopScrollRect.dataSource = this;
        loopScrollRect.totalCount = newCount;
        if (refill)
        {
            loopScrollRect.RefillCells();
        }
    }

    public void RefreshCells()
    {
        loopScrollRect.RefreshCells();
    }

    public void RefillCells()
    {
        loopScrollRect.RefillCells();
    }

    public void ScrollToCellWithinTime(int index, float time = 0.5f)
    {
        loopScrollRect.ScrollToCellWithinTime(index, time);
    }

    #endregion

    private void HandleScrollValueChanged(Vector2 position)
    {
        OnScrollValueChanged?.Invoke(position);
    }
}
