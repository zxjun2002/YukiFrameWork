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
        public RedPointChangeDelegate OnRedPointChange;

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
        private RedPointNode root;
        // 用于挂起订阅：节点 key -> 回调列表
        private Dictionary<string, List<RedPointNode.RedPointChangeDelegate>> pendingSubscriptions = new Dictionary<string, List<RedPointNode.RedPointChangeDelegate>>();
        
        public RedPointAgg()
        {
            root = new RedPointNode(RedPointKey.Root);
        }
        
        public RedPointNode AddNode(string key,bool IsUnique = false)
        {
            //这部分代表是唯一节点，如果有的话那么就不增加个数了
            if (IsUnique && FindNode(key) != null)
            {
                return null;
            }
            string[] keys = key.Split('|');
            RedPointNode curNode = root;
            curNode.redNum += 1;
            curNode.OnRedPointChange?.Invoke(curNode.redNum);
            // 用于累计完整路径（例如 "Play|Level1|SHOP"）
            string currentAccumulatedKey = "";
            foreach (string k in keys)
            {
                currentAccumulatedKey = string.IsNullOrEmpty(currentAccumulatedKey) ? k : currentAccumulatedKey + "|" + k;
                
                if (!curNode.children.ContainsKey(k))
                {
                    curNode.children.Add(k, new RedPointNode(k));
                }
                curNode = curNode.children[k];
                curNode.redNum += 1;
                // 如果之前有挂起的订阅，则立即注册
                if (pendingSubscriptions.ContainsKey(currentAccumulatedKey))
                {
                    foreach (var callback in pendingSubscriptions[currentAccumulatedKey])
                    {
                        curNode.OnRedPointChange += callback;
                    }
                    pendingSubscriptions.Remove(currentAccumulatedKey);
                }
                curNode.OnRedPointChange?.Invoke(curNode.redNum);
            }
            return curNode;
        }

        public RedPointNode FindNode(string key)
        {
            string[] keys = key.Split('|');
            RedPointNode curNode = root;
            foreach (string k in keys)
            {
                if (!curNode.children.ContainsKey(k))
                {
                    return null;
                }
                curNode = curNode.children[k];
            }
            return curNode;
        }

                /// <summary>
        /// 删除一个红点
        /// </summary>
        public void DeleteNode(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (FindNode(key) == null) return; // 不存在直接返回
            DeleteNode(key, root);
        }

        /// <summary>
        /// 沿 key 递归删除“一次计数”；
        /// 采用“后序”更新：先删叶子，再回溯到父节点做 --redNum 和剪枝。
        /// </summary>
        private RedPointNode DeleteNode(string key, RedPointNode node)
        {
            // 走到目标节点（没有后续分段）
            if (string.IsNullOrEmpty(key))
            {
                int before = node.redNum;
                node.redNum = Mathf.Clamp(node.redNum - 1, 0, before);
                if (node.redNum != before)
                    node.OnRedPointChange?.Invoke(node.redNum);
                return node;
            }

            // 取 head|tail
            int sep = key.IndexOf('|');
            string head = sep < 0 ? key : key.Substring(0, sep);
            string tail = sep < 0 ? string.Empty : key.Substring(sep + 1);

            if (!node.children.TryGetValue(head, out var child))
            {
                // 理论上不会到这（外面已检查存在性），稳妥起见直接返回
                return node;
            }

            // 先递归到子节点
            DeleteNode(tail, child);

            // 如果子节点已经空了，就从“父的 children”里移除它（✅ 正确的剪枝位置）
            if (child.redNum == 0 && child.children.Count == 0)
            {
                node.children.Remove(head);
            }

            // 再回溯更新当前节点计数并广播
            int before2 = node.redNum;
            node.redNum = Mathf.Clamp(node.redNum - 1, 0, before2);
            if (node.redNum != before2)
                node.OnRedPointChange?.Invoke(node.redNum);

            return node;
        }

        //直接清空一个节点
        public void ClearRedNode(string key)
        {
            if (FindNode(key) == null) return;
            ClearRedNode(key, root);
        }

        /// <summary>
        /// 递归清空 key 对应分支，返回本分支被清除的总红点数；
        /// 父节点用它来做 --redNum，并按需移除子节点。
        /// </summary>
        private int ClearRedNode(string key, RedPointNode node)
        {
            if (string.IsNullOrEmpty(key))
            {
                int diff = node.redNum;
                node.redNum = 0;
                node.children.Clear();
                node.OnRedPointChange?.Invoke(0);
                return diff;
            }

            int sep = key.IndexOf('|');
            string head = sep < 0 ? key : key.Substring(0, sep);
            string tail = sep < 0 ? string.Empty : key.Substring(sep + 1);

            if (!node.children.TryGetValue(head, out var child))
                return 0;

            int cleared = ClearRedNode(tail, child);

            // 子节点若已清空则剪枝
            if (child.redNum == 0 && child.children.Count == 0)
                node.children.Remove(head);

            // 用 cleared 来回溯扣减当前节点并广播
            int before = node.redNum;
            node.redNum = Mathf.Clamp(node.redNum - cleared, 0, before);
            if (node.redNum != before)
                node.OnRedPointChange?.Invoke(node.redNum);

            return cleared;
        }

        public void SetCallback(string key, RedPointNode.RedPointChangeDelegate cb)
        {
            RedPointNode node = FindNode(key);
            if (node == null)
            {
                // 节点尚未创建，将回调挂起
                if (!pendingSubscriptions.ContainsKey(key))
                {
                    pendingSubscriptions[key] = new List<RedPointNode.RedPointChangeDelegate>();
                }
                if (pendingSubscriptions[key].Contains(cb))
                {
                    GameLogger.LogError("重复添加的红点委托");
                    return;
                }
                pendingSubscriptions[key].Add(cb);
                //可选比如服务端点亮的红点,消息早于回调,直接调用回调
                cb?.Invoke(0);
                return;
            }
            // 判断节点已存在的回调中是否已包含该 cb
            if (node.OnRedPointChange != null && node.OnRedPointChange.GetInvocationList().Any(existing => existing.Equals(cb)))
            {
                GameLogger.LogError("重复添加的红点委托");
                return;
            }
            node.OnRedPointChange += cb;
            //可选比如服务端点亮的红点,消息早于回调,直接调用回调
            node.OnRedPointChange?.Invoke(node.redNum);
        }
        
        public void DeleteCallback(string key, RedPointNode.RedPointChangeDelegate cb)
        {
            RedPointNode node = FindNode(key);
            if (node == null)
            {
                // 节点没创建,回收回调
                if (pendingSubscriptions.ContainsKey(key))
                {
                    pendingSubscriptions[key].Remove(cb);
                }
                cb?.Invoke(0);
                return;
            }
            node.OnRedPointChange -= cb;
        }

        public int GetRedPointNum(string key)
        {
            RedPointNode node = FindNode(key);
            if (node == null)
            {
                return 0;
            }
            return node.redNum;
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