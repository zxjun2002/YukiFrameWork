using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Domain
{
    public class RedPointNode
    {
        public int redNum;
        public string strKey;
        public Dictionary<string, RedPointNode> children;
        public delegate void RedPointChangeDelegate(int redNum);

        public RedPointNode(string key)
        {
            strKey = key;
            children = new Dictionary<string, RedPointNode>();
        }
    }
    
    /// <summary>
    /// 红点
    /// </summary>
    public class RedPointAgg
    {
        private readonly RedPointNode root = new RedPointNode(RedPointKey.Root);
        private readonly Dictionary<string, HashSet<RedPointNode.RedPointChangeDelegate>> _listeners = new();

        private static string Norm(string key) => key?.Trim();

        private void Notify(string key, int value)
        {
            if (!_listeners.TryGetValue(key, out var set) || set.Count == 0) return;

            // ★ 用快照防止“回调里增删监听”导致集合在 foreach 中被修改
            var snapshot = set.ToArray();
            foreach (var cb in snapshot)
            {
                try { cb?.Invoke(value); }
                catch (Exception ex) { Debug.LogError($"[RedDot] listener error key={key}, val={value}\n{ex}"); }
            }
        }

        private RedPointNode EnsurePath(string key, out List<(string accKey, RedPointNode node)> path)
        {
            path = new List<(string, RedPointNode)>();
            var cur = root;
            string acc = string.Empty;

            foreach (var seg in key.Split('|'))
            {
                acc = string.IsNullOrEmpty(acc) ? seg : $"{acc}|{seg}";
                if (!cur.children.TryGetValue(seg, out var child))
                {
                    child = new RedPointNode(seg);
                    cur.children.Add(seg, child);
                }
                cur = child;
                path.Add((acc, cur));
            }
            return cur;
        }

        /// <summary>
        /// 添加一个节点
        /// </summary>
        public RedPointNode AddNode(string key, bool isUnique = false)
        {
            key = Norm(key);
            if (string.IsNullOrEmpty(key)) return null;

            // 唯一：该完整 key 已存在则不再 +1
            if (isUnique && FindNode(key) != null) return null;

            // 根 +1 & 通知
            root.redNum += 1;
            Notify(RedPointKey.Root, root.redNum);

            // 逐层 +1 & 通知
            EnsurePath(key, out var path);
            foreach (var (accKey, node) in path)
            {
                node.redNum += 1;
                Notify(accKey, node.redNum);
            }
            return path.Count > 0 ? path[^1].node : root;
        }

        /// <summary>
        /// 删除一个节点
        /// </summary>
        public void DeleteNode(string key)
        {
            key = Norm(key);
            if (string.IsNullOrEmpty(key) || FindNode(key) == null) return;
            DeleteRec(key, root, "", isRoot: true);
        }

        private void DeleteRec(string key, RedPointNode node, string accKey, bool isRoot)
        {
            if (string.IsNullOrEmpty(key))
            {
                int before = node.redNum;
                node.redNum = Mathf.Clamp(before - 1, 0, before);
                Notify(isRoot ? RedPointKey.Root : accKey, node.redNum);
                return;
            }

            int sep = key.IndexOf('|');
            string head = sep < 0 ? key : key[..sep];
            string tail = sep < 0 ? "" : key[(sep + 1)..];

            if (!node.children.TryGetValue(head, out var child)) return;

            string childAcc = string.IsNullOrEmpty(accKey) ? head : $"{accKey}|{head}";
            DeleteRec(tail, child, childAcc, isRoot: false);

            // 剪枝逻辑：计数为 0 且没有子节点 → 移除（监听与节点已解耦，剪不剪都不会丢监听）
            if (child.redNum == 0 && child.children.Count == 0)
                node.children.Remove(head);

            int before2 = node.redNum;
            node.redNum = Mathf.Clamp(before2 - 1, 0, before2);
            Notify(isRoot ? RedPointKey.Root : accKey, node.redNum);
        }

        /// <summary>
        /// 直接清空一个节点
        /// </summary>
        public void ClearRedNode(string key)
        {
            key = Norm(key);
            if (FindNode(key) == null) return;
            ClearRec(key, root, "", isRoot: true);
        }

        // 将分支全部置 0；按需剪枝（这里选择“叶子即剪”，更干净；监听不受影响）
        private int ClearRec(string key, RedPointNode node, string accKey, bool isRoot)
        {
            if (string.IsNullOrEmpty(key))
            {
                int diff = node.redNum;
                node.redNum = 0;
                Notify(isRoot ? RedPointKey.Root : accKey, 0);

                // 清掉整个子树
                var toRemove = new List<string>();
                foreach (var kv in node.children)
                {
                    string childAcc = string.IsNullOrEmpty(accKey) ? kv.Key : $"{accKey}|{kv.Key}";
                    ClearSubtree(kv.Value, childAcc, toRemove);
                }
                foreach (var k in toRemove) node.children.Remove(k);

                return diff;
            }

            int sep = key.IndexOf('|');
            string head = sep < 0 ? key : key[..sep];
            string tail = sep < 0 ? "" : key[(sep + 1)..];

            if (!node.children.TryGetValue(head, out var child)) return 0;

            string childAccKey = string.IsNullOrEmpty(accKey) ? head : $"{accKey}|{head}";
            int cleared = ClearRec(tail, child, childAccKey, isRoot: false);

            if (child.redNum == 0 && child.children.Count == 0)
                node.children.Remove(head);

            int before = node.redNum;
            node.redNum = Mathf.Clamp(before - cleared, 0, before);
            Notify(isRoot ? RedPointKey.Root : accKey, node.redNum);
            return cleared;
        }

        private void ClearSubtree(RedPointNode node, string accKey, List<string> removableKeys)
        {
            node.redNum = 0;
            Notify(accKey, 0);

            var innerRemovals = new List<string>();
            foreach (var kv in node.children)
            {
                string childAcc = $"{accKey}|{kv.Key}";
                ClearSubtree(kv.Value, childAcc, innerRemovals);
            }
            foreach (var k in innerRemovals) node.children.Remove(k);

            // 自己已经 0 且无子 → 可删
            if (node.children.Count == 0)
                removableKeys.Add(node.strKey);
        }

        public RedPointNode FindNode(string key)
        {
            key = Norm(key);
            if (string.IsNullOrEmpty(key)) return null;

            var cur = root;
            foreach (var seg in key.Split('|'))
            {
                if (!cur.children.TryGetValue(seg, out var child)) return null;
                cur = child;
            }
            return cur;
        }

        public int GetRedPointNum(string key)
        {
            key = Norm(key);
            if (string.IsNullOrEmpty(key)) return root.redNum;
            var n = FindNode(key);
            return n?.redNum ?? 0;
        }
        
        public void SetCallback(string key, RedPointNode.RedPointChangeDelegate cb)
        {
            key = Norm(key);
            if (string.IsNullOrEmpty(key) || cb == null) return;

            if (!_listeners.TryGetValue(key, out var set))
                _listeners[key] = set = new HashSet<RedPointNode.RedPointChangeDelegate>();

            if (!set.Add(cb))
            {
                GameLogger.LogWarning("重复添加的红点委托");
                return;
            }
            
            int val = GetRedPointNum(key);
            try { cb(val); } catch (Exception ex) { Debug.LogError(ex); }
        }

        public void DeleteCallback(string key, RedPointNode.RedPointChangeDelegate cb)
        {
            key = Norm(key);
            if (string.IsNullOrEmpty(key) || cb == null) return;

            if (_listeners.TryGetValue(key, out var set))
            {
                set.Remove(cb);
                if (set.Count == 0) _listeners.Remove(key);
            }
        }
    }

    public class RedPointKey
    {
        public const string Root = "Root";

        public const string Play = "Play";
        public const string Play_LEVEL1 = "Play|Level1";
        public const string Play_LEVEL1_HOME = "Play|Level1|HOME";
        public const string Play_LEVEL1_SHOP = "Play|Level1|SHOP";
        public const string Play_LEVEL2 = "Play|Level2";
        public const string Play_LEVEL2_HOME = "Play|Level2|HOME";
        public const string Play_LEVEL2_SHOP = "Play|Level2|SHOP";
    }
}