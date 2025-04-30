using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(LoopScrollRectMulti))]
[DisallowMultipleComponent]
public class UIListMulti : MonoBehaviour, LoopScrollPrefabSource, LoopScrollMultiDataSource
{
    [Header("Prefab 模式设置")] public bool isUseMultiPrefabs = false;
    [Tooltip("单Prefab模式下使用")] public GameObject singlePrefab;
    [Tooltip("多Prefab模式下使用")] public List<UIListItem> multiPrefabs;

    // 内部 LoopScrollRectMulti 引用
    private LoopScrollRectMulti loopScrollRect;

    // 对象池：单Prefab
    private Stack<Transform> poolSingle = new Stack<Transform>();
    // 对象池：多Prefab，key=prefab名字
    private Dictionary<string, Stack<Transform>> poolMulti = new Dictionary<string, Stack<Transform>>();

    /// <summary>滚动位置变化事件</summary>
    public UnityEvent<Vector2> OnScrollValueChanged;
    // 索引数据委托
    public Func<int, UIListItemData> SetIndexData;

    #region LoopScrollPrefabSource 实现

    public GameObject GetObject(int index)
    {
        GameObject go;
        UIListItem listItem;

        if (!isUseMultiPrefabs)
        {
            // 单Prefab模式
            if (poolSingle.Count == 0)
            {
                go = Instantiate(singlePrefab);
            }
            else
            {
                var tr = poolSingle.Pop();
                go = tr.gameObject;
                go.SetActive(true);
            }
            listItem = go.GetComponent<UIListItem>();
        }
        else
        {
            // 多Prefab模式
            var data = SetIndexData?.Invoke(index);
            string prefabName = data?.GetType().Name.Replace("Data", "");
            var prefab = multiPrefabs.Find(p => p.GetType().Name == prefabName).gameObject;

            if (!poolMulti.TryGetValue(prefab.name, out var stack))
            {
                stack = new Stack<Transform>();
                poolMulti[prefab.name] = stack;
            }

            if (stack.Count == 0)
            {
                go = Instantiate(prefab);
                go.name = prefab.name;
            }
            else
            {
                var tr = stack.Pop();
                go = tr.gameObject;
                go.SetActive(true);
            }
            listItem = go.GetComponent<UIListItem>();
        }

        if (listItem != null)
        {
            // 生命周期：初始化
            listItem.AddInit();
            listItem.init?.Invoke();

            // 生命周期：准备显示
            listItem.AddOnShow();
        }

        return go;
    }

    public void ReturnObject(Transform trans)
    {
        var listItem = trans.GetComponent<UIListItem>();
        if (listItem != null)
        {
            // 生命周期：隐藏前
            listItem.onHide?.Invoke();
            listItem.ResetShowState();
        }
        trans.gameObject.SetActive(false);
        trans.SetParent(this.transform, false);

        if (!isUseMultiPrefabs)
        {
            poolSingle.Push(trans);
        }
        else
        {
            if (!poolMulti.TryGetValue(trans.name, out var stack))
            {
                stack = new Stack<Transform>();
                poolMulti[trans.name] = stack;
            }
            stack.Push(trans);
        }
    }

    #endregion

    #region LoopScrollMultiDataSource 实现

    public void ProvideData(Transform t, int idx)
    {
        var listItem = t.GetComponent<UIListItem>();
        if (listItem == null)
        {
            Debug.LogError($"[UIListMulti] 找不到 UIListItem，idx={idx}");
            return;
        }
        
        // 生命周期：再次进入显示阶段
        listItem.AddOnShow();
        // 生命周期：准备展示数据
        listItem.AddShowData();
        // 生命周期：准备隐藏
        listItem.AddOnHide();

        var data = SetIndexData?.Invoke(idx);
        if (data != null)
        {
            listItem.TryInvokeOnShow();
            listItem.showData.Invoke(data);
        }
        else
        {
            Debug.LogError($"[UIListMulti] 数据为空，idx={idx}");
        }
    }

    #endregion

    #region 对外接口

    public void SetCount(int newCount)
    {
        if (loopScrollRect == null)
        {
            loopScrollRect = GetComponent<LoopScrollRectMulti>();
            loopScrollRect.onValueChanged.RemoveAllListeners();
            loopScrollRect.onValueChanged.AddListener(HandleScrollValueChanged);
        }
        loopScrollRect.prefabSource = this;
        loopScrollRect.dataSource = this;
        loopScrollRect.totalCount = newCount;
        loopScrollRect.RefillCells();
    }

    public void UpdateContent() => loopScrollRect.RefreshCells();

    public void ScrollToCellWithinTime(int index, float time = 0.5f)
        => loopScrollRect.ScrollToCellWithinTime(index, time);

    public async UniTask ScrollToCellWithinTimeAsync(int index, float time = 0.5f, CancellationToken cts = default)
        => await loopScrollRect.ScrollToCellWithinTimeAsync(index, time, cts);

    #endregion

    #region 生命周期管理

    private void OnDestroy()
    {
        if (loopScrollRect != null)
            loopScrollRect.onValueChanged.RemoveAllListeners();
    }

    private void HandleScrollValueChanged(Vector2 pos)
        => OnScrollValueChanged?.Invoke(pos);

    #endregion
}
