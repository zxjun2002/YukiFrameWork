using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal sealed partial class UGUIScriptGeneratorContext
{
    internal enum ConflictType
    {
        DuplicateNameDifferentObject,
        DuplicateSameObjectSameType,
        FieldNameConflict
    }

    internal class ConflictInfo
    {
        public ConflictType type;
        public string title;
        public string message;
        public GameObject oldObj;
        public GameObject newObj;
    }

    internal ConflictInfo ValidateConflicts()
    {
        // 1) 不同物体同名
        var nameGroups = uiElements
            .Where(e => e != null && e.uiObject != null)
            .GroupBy(e => e.uiObject.name);

        foreach (var g in nameGroups)
        {
            var objs = g.Select(x => x.uiObject).Distinct().ToList();
            if (objs.Count <= 1) continue;

            var a = objs[0];
            var b = objs[1];

            return new ConflictInfo
            {
                type = ConflictType.DuplicateNameDifferentObject,
                title = "生成失败：不同物体同名冲突",
                oldObj = a,
                newObj = b,
                message =
                    $"名称冲突：\"{g.Key}\"\n\n" +
                    $"物体A路径：{GetHierarchyPath(a)}\n" +
                    $"物体B路径：{GetHierarchyPath(b)}\n\n" +
                    "处理建议：请修改其中一个 GameObject 的名字（不同物体不允许同名）。"
            };
        }

        // 2) 同物体同类型重复
        var dupSameObjSameType = uiElements
            .Where(e => e != null && e.uiObject != null)
            .GroupBy(e => (e.uiObject, e.selectedComponentType))
            .FirstOrDefault(g => g.Count() > 1);

        if (dupSameObjSameType != null)
        {
            var first = dupSameObjSameType.First();
            return new ConflictInfo
            {
                type = ConflictType.DuplicateSameObjectSameType,
                title = "生成失败：同物体同类型重复",
                oldObj = first.uiObject,
                message =
                    $"物体：{first.uiObject.name}\n" +
                    $"路径：{GetHierarchyPath(first.uiObject)}\n" +
                    $"类型：{first.selectedComponentType}\n\n" +
                    "处理建议：删除重复条目（同一物体同一类型只能出现一次）。"
            };
        }

        // 3) 字段名冲突
        var groupsByField = uiElements
            .Where(e => e != null && e.uiObject != null)
            .GroupBy(e => e.fieldName);

        foreach (var g in groupsByField)
        {
            if (string.IsNullOrEmpty(g.Key)) continue;
            var list = g.ToList();
            if (list.Count <= 1) continue;

            var a = list[0].uiObject;
            var b = list[1].uiObject;

            return new ConflictInfo
            {
                type = ConflictType.FieldNameConflict,
                title = "生成失败：字段名冲突",
                oldObj = a,
                newObj = b,
                message =
                    $"字段名冲突：\"{g.Key}\"\n\n" +
                    $"物体A路径：{GetHierarchyPath(a)}\n" +
                    $"物体B路径：{GetHierarchyPath(b)}\n\n" +
                    "处理建议：修改新控件名字/类型（老字段名不会被改动）。"
            };
        }

        return null;
    }

    internal void ShowConflictDialog(ConflictInfo conflict)
    {
        if (conflict == null) return;

        string btnOld = conflict.oldObj != null ? "选中物体A" : "确定";
        string btnNew = conflict.newObj != null ? "选中物体B" : "关闭";
        string btnClose = "关闭";

        int result = EditorUtility.DisplayDialogComplex(
            conflict.title,
            conflict.message,
            btnOld,
            btnNew,
            btnClose
        );

        if (result == 0 && conflict.oldObj != null) SelectAndPing(conflict.oldObj);
        else if (result == 1 && conflict.newObj != null) SelectAndPing(conflict.newObj);
    }

    internal bool IsDuplicateSameObjSameType(UIElement element)
    {
        if (element == null || element.uiObject == null) return false;
        return uiElements.Count(e =>
            e != null &&
            e.uiObject == element.uiObject &&
            e.selectedComponentType == element.selectedComponentType
        ) > 1;
    }

    internal static GameObject FindRootNode(GameObject uiElement) => UGUIBindingAccess.FindRoot(uiElement);

    internal void CheckRootNodeConsistency()
    {
        isRootNodeChecked = false;
        if (uiElements.Count == 0)
        {
            isRootNodeChecked = rootNode != null;
            return;
        }

        GameObject initialRoot = null;
        foreach (var e in uiElements)
        {
            if (e != null && e.uiObject != null)
            {
                initialRoot = FindRootNode(e.uiObject);
                break;
            }
        }

        if (initialRoot == null)
        {
            Debug.LogError("未找到当前UI元素符合条件的根节点");
            return;
        }

        rootNode = initialRoot;

        foreach (var element in uiElements)
        {
            if (element == null || element.uiObject == null) continue;

            GameObject elementRoot = FindRootNode(element.uiObject);
            if (elementRoot != rootNode)
            {
                isRootNodeChecked = false;
                Debug.LogError($"UI Element {element.uiObject.name} 有不同的节点");
                element.uiObject = null;
                return;
            }
        }

        isRootNodeChecked = true;
        RestoreBindings();
        Debug.Log($"All UI elements have the same root node: {rootNode.name}");
    }

    internal string GetHierarchyPath(GameObject go)
    {
        if (go == null) return "(null)";
        if (rootNode == null) return go.name;

        Transform t = go.transform;
        Transform root = rootNode.transform;

        var stack = new Stack<string>();
        while (t != null)
        {
            stack.Push(t.name);
            if (t == root) break;
            t = t.parent;
        }

        return string.Join("/", stack);
    }

    internal static void SelectAndPing(GameObject go)
    {
        if (go == null) return;

        Selection.activeObject = go;
        EditorGUIUtility.PingObject(go);

        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();
    }

    internal List<GameObject> GetSameNameOthers(GameObject current)
    {
        if (current == null) return new List<GameObject>();
        string n = current.name;

        return uiElements
            .Where(e => e != null && e.uiObject != null && e.uiObject != current && e.uiObject.name == n)
            .Select(e => e.uiObject)
            .Distinct()
            .ToList();
    }

    internal void SelectOtherSameName(GameObject current)
    {
        var others = GetSameNameOthers(current);
        if (others.Count == 0) return;

        if (others.Count == 1)
        {
            SelectAndPing(others[0]);
            return;
        }

        var menu = new GenericMenu();
        foreach (var go in others)
        {
            string label = $"{GetHierarchyPath(go)}";
            menu.AddItem(new GUIContent(label), false, () => SelectAndPing(go));
        }
        menu.ShowAsContext();
    }
}
