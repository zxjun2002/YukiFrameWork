using System.Collections.Generic;
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

        public void DeleteNode(string key)
        {
            if (FindNode(key) == null)
            {
                return;
            }
            DeleteNode(key, root);
        }

        private RedPointNode DeleteNode(string key, RedPointNode node)
        {
            string[] keys = key.Split('|');
            if (key=="" || keys.Length == 0)
            {
                node.redNum = Mathf.Clamp(node.redNum - 1, 0, node.redNum);
                node.OnRedPointChange?.Invoke(node.redNum);
                return node;
            }
            string newKey = string.Join("|", keys, 1, keys.Length - 1);
            RedPointNode curNode = DeleteNode(newKey, node.children[keys[0]]);

            node.redNum = Mathf.Clamp(node.redNum - 1, 0, node.redNum);
            node.OnRedPointChange?.Invoke(node.redNum);

            if (curNode.children.Count > 0)
            {
                foreach (RedPointNode child in curNode.children.Values)
                {
                    if (child.redNum == 0)
                    {
                        child.children.Remove(child.strKey);
                    }
                }
            }
            return node;
        }
        
        //直接清空一个节点
        public void ClearRedNode(string key)
        {
            string[] keys = key.Split('|');
            RedPointNode curNode = root;
            Stack<RedPointNode> stack = new Stack<RedPointNode>();

            foreach (string k in keys)
            {
                if (!curNode.children.ContainsKey(k))
                {
                    return;
                }
                stack.Push(curNode);
                curNode = curNode.children[k];
            }

            int diff = curNode.redNum; // Store the redNum to adjust parent nodes.
            curNode.redNum = 0;
            curNode.OnRedPointChange?.Invoke(0);

            if (stack.Count > 0)
            {
                RedPointNode parent = stack.Pop();
                parent.children.Remove(keys[^1]);

                while (stack.Count > 0)
                {
                    parent.redNum = Mathf.Clamp(parent.redNum - diff, 0, parent.redNum);
                    parent.OnRedPointChange?.Invoke(parent.redNum);

                    if (parent.redNum == 0 && stack.Count > 0)
                    {
                        RedPointNode grandParent = stack.Pop();
                        grandParent.children.Remove(parent.strKey);
                        parent = grandParent;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public void SetCallBack(string key, RedPointNode.RedPointChangeDelegate cb)
        {
            RedPointNode node = FindNode(key);
            if (node == null)
            {
                // 节点尚未创建，将回调挂起
                if (!pendingSubscriptions.ContainsKey(key))
                {
                    pendingSubscriptions[key] = new List<RedPointNode.RedPointChangeDelegate>();
                }
                pendingSubscriptions[key].Add(cb);
                //可选比如服务端点亮的红点,消息早于回调,直接调用回调
                cb?.Invoke(0);
                return;
            }
            node.OnRedPointChange += cb;
            //可选比如服务端点亮的红点,消息早于回调,直接调用回调
            node.OnRedPointChange?.Invoke(node.redNum);
        }
        
        public void DelteCallback(string key, RedPointNode.RedPointChangeDelegate cb)
        {
            RedPointNode node = FindNode(key);
            if (node == null)
            {
                return;
            }
            node.OnRedPointChange -= cb;
        }

        public int GetRedpointNum(string key)
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