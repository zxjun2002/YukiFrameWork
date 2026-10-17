using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class UGUIScriptGenerator : EditorWindow
{
    [SerializeField] private UGUIScriptGeneratorContext context = new UGUIScriptGeneratorContext();
    private Vector2 scrollPosition;
    private GUIStyle dropHintStyle;

    public List<UIElement> uiElements => context.uiElements;
    public string scriptOutputPath { get => context.scriptOutputPath; set => context.scriptOutputPath = value; }
    public static string NameSpaceName => "Yuki";
    private GameObject rootNode => context.rootNode;
    private bool isRootNodeChecked { get => context.isRootNodeChecked; set => context.isRootNodeChecked = value; }

    private void OnEnable() => UGUIBindingAccess.Changed += OnConfigurationChanged;
    private void OnDisable() => UGUIBindingAccess.Changed -= OnConfigurationChanged;

    private void OnConfigurationChanged(UGUIScriptGeneratorBindings settings)
    {
        if (context.rootNode == null || UGUIBindingAccess.PanelTypeName(context.rootNode) != settings.panelTypeName) return;
        context.LoadRoot(context.rootNode);
        Repaint();
    }

    private void PersistConfiguration()
    {
        if (!context.isRootNodeChecked || context.rootNode == null) return;
        try { context.SaveConfiguration(); }
        catch (Exception exception) { Debug.LogWarning("[UGUI ScriptGen] " + exception.Message); }
    }

    [MenuItem("Tool/UGUI Script Generator")]
    public static void ShowWindow() => GetWindow<UGUIScriptGenerator>("UGUI Script Generator");

    [MenuItem("GameObject/工具/Generate UI Script for UIPanel", false, 10)]
    private static void GenerateUIScriptForSelectedPanel()
    {
        var selected = Selection.activeGameObject;
        if (selected == null || selected.GetComponent<BasePanel>() == null)
        {
            Debug.LogError("请选择已挂载 BasePanel 的面板根节点。");
            return;
        }
        OpenForRoot(selected);
    }

    internal static void OpenForRoot(GameObject root)
    {
        var window = GetWindow<UGUIScriptGenerator>("UGUI Script Generator");
        window.context.LoadRoot(root);
        window.Repaint();
    }

    private void OnGUI()
    {
        // 在滚动区改变坐标、ObjectField 消费事件之前统一处理整个窗口的拖拽。
        if (HandleDragAndDrop()) return;

        bool hasError = false;
        List<int> elementsToRemove = new List<int>();
        // 整个窗口共用滚动区，避免空列表独占剩余高度、把操作按钮挤到底部。
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (uiElements.Count == 0)
            EditorGUILayout.HelpBox("暂无 UI 元素，可点击添加，或将控件拖入窗口任意位置。", MessageType.Info);

        for (int i = 0; i < uiElements.Count; i++)
        {
            var element = uiElements[i];

            GUILayout.BeginHorizontal();

            var newUiObject = (GameObject)EditorGUILayout.ObjectField(element.uiObject, typeof(GameObject), true);
            if (newUiObject != element.uiObject)
            {
                element.uiObject = newUiObject;

                if (element.uiObject != null)
                {
                    if (string.IsNullOrEmpty(element.selectedComponentType))
                        element.selectedComponentType = UGUIScriptGeneratorContext.GetDefaultComponentType(element.uiObject);

                    // 新控件：字段名总是 name+type
                    context.AutoUpdateFieldNameForNew(element);
                    context.CheckRootNodeConsistency();
                    PersistConfiguration();
                }
            }

            if (element.uiObject != null)
            {
                string[] availableTypes = element.GetAvailableComponentTypes();
                int currentIndex = Array.IndexOf(availableTypes, element.selectedComponentType);
                if (currentIndex == -1) currentIndex = 0;

                GUILayout.Label("类型", GUILayout.Width(30));

                bool prevEnabled = GUI.enabled;
                GUI.enabled = prevEnabled && !element.isLocked;

                int newIndex = EditorGUILayout.Popup(currentIndex, availableTypes);
                if (!element.isLocked && newIndex != currentIndex)
                {
                    // 同物体同类型重复（即时阻止）
                    if (uiElements.Any(e => e != null && e != element && e.uiObject == element.uiObject && e.selectedComponentType == availableTypes[newIndex]))
                    {
                        EditorUtility.DisplayDialog(
                            "重复的 UI 元素",
                            $"物体 “{element.uiObject.name}” 的类型 “{availableTypes[newIndex]}” 已经存在！",
                            "确定"
                        );
                    }
                    else
                    {
                        element.UpdateSelectedComponentType(availableTypes[newIndex]);
                        context.AutoUpdateFieldNameForNew(element);
                        PersistConfiguration();
                    }
                }

                GUI.enabled = prevEnabled;

                GUILayout.Label(element.isLocked ? "锁定" : "新", GUILayout.Width(28));
                GUILayout.Label($"字段: {element.fieldName}", GUILayout.MinWidth(240));
                DrawEventSelector(element);
            }

            if (GUILayout.Button("移除", GUILayout.Width(60)))
                elementsToRemove.Add(i);

            GUILayout.EndHorizontal();

            if (context.IsDuplicateSameObjSameType(element))
            {
                EditorGUILayout.HelpBox($"!! 同一物体“{element.uiObject?.name}”同一类型“{element.selectedComponentType}”重复", MessageType.Error);
                hasError = true;
            }

            if (element.uiObject != null)
            {
                var others = context.GetSameNameOthers(element.uiObject);
                if (others.Count > 0)
                {
                    EditorGUILayout.HelpBox($"!! 存在不同物体同名：“{element.uiObject.name}”", MessageType.Error);
                    hasError = true;

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    if (GUILayout.Button("定位当前", GUILayout.Width(80)))
                        UGUIScriptGeneratorContext.SelectAndPing(element.uiObject);

                    if (GUILayout.Button("定位同名", GUILayout.Width(80)))
                        context.SelectOtherSameName(element.uiObject);
                    GUILayout.EndHorizontal();
                }
            }
        }

        foreach (int index in elementsToRemove.OrderByDescending(i => i))
        {
            uiElements.RemoveAt(index);
            context.CheckRootNodeConsistency();
            PersistConfiguration();
        }
        if (GUILayout.Button("添加UI元素"))
        {
            uiElements.Add(new UIElement("", null) { isLocked = false });
            isRootNodeChecked = false;
        }

        GUILayout.Label(rootNode != null ? $"当前根节点: {rootNode.name}" : "没有选中的根节点");

        if (GUILayout.Button("检查根节点一致性"))
        {
            context.CheckRootNodeConsistency();
        }

        // 输出路径持久化
        string previousPath = EditorPrefs.GetString("ScriptOutputPath", "Assets/Scripts/UI/");
        if (string.IsNullOrEmpty(scriptOutputPath))
            scriptOutputPath = previousPath;

        GUILayout.BeginHorizontal();
        scriptOutputPath = EditorGUILayout.TextField("脚本输出路径", scriptOutputPath);
        if (GUILayout.Button("选择路径", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Output Folder", scriptOutputPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                scriptOutputPath = "Assets" + path.Substring(Application.dataPath.Length);
                EditorPrefs.SetString("ScriptOutputPath", scriptOutputPath);
            }
        }
        GUILayout.EndHorizontal();

        GUI.enabled = isRootNodeChecked && !hasError;
        if (GUILayout.Button("生成脚本"))
        {
            context.GenerateScript();
        }
        GUI.enabled = true;

        DrawDropHint();
        EditorGUILayout.EndScrollView();
    }

    private void DrawEventSelector(UIElement element)
    {
        context.InitializeEvents(element);
        var available = UGUIScriptGeneratorEvents.GetEvents(UGUIScriptGeneratorContext.GetSelectedComponentTypeFromObject(element));
        var selectedEvents = element.events.Where(e => e.enabled).ToList();
        if (available.Count == 0 && selectedEvents.Count == 0) return;
        string summary = selectedEvents.Count == 0 ? "不监听" : string.Join(", ", selectedEvents.Select(e => e.eventName));
        var content = new GUIContent("监听: " + summary, $"稳定 Key: {element.bindingKey}\n勾选事件后生成对应回调。取消勾选只移除绑定，保留方法体。");
        if (!GUILayout.Button(content, EditorStyles.popup, GUILayout.Width(220))) return;
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("不监听"), selectedEvents.Count == 0, () => { element.events.ForEach(e => e.enabled = false); PersistConfiguration(); Repaint(); });
        menu.AddSeparator("");
        foreach (var descriptor in available)
        {
            var selected = element.events.FirstOrDefault(e => e.eventName == descriptor.name);
            menu.AddItem(new GUIContent(descriptor.Label), selected != null && selected.enabled, () =>
            {
                if (selected != null) selected.enabled = !selected.enabled;
                else element.events.Add(context.CreateEventBinding(element, descriptor.name));
                PersistConfiguration();
                Repaint();
            });
        }
        foreach (var missing in selectedEvents.Where(e => available.All(descriptor => descriptor.name != e.eventName)))
            menu.AddItem(new GUIContent(missing.eventName + " (已失效，点击取消)"), true, () => { missing.enabled = false; PersistConfiguration(); Repaint(); });
        menu.ShowAsContext();
    }

    private void DrawDropHint()
    {
        if (dropHintStyle == null)
            dropHintStyle = new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
        GUILayout.Space(4);
        GUILayout.Box("将 UI 对象拖入窗口任意位置\n支持批量拖入，自动识别组件类型", dropHintStyle,
            GUILayout.Height(160), GUILayout.ExpandWidth(true));
    }

    private bool HandleDragAndDrop()
    {
        Event evt = Event.current;
        if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return false;
        if (!new Rect(Vector2.zero, position.size).Contains(evt.mousePosition)) return false;

        var draggedObjects = DragAndDrop.objectReferences
            .Where(obj => obj is GameObject || obj is Component).ToArray();
        DragAndDrop.visualMode = draggedObjects.Length > 0 ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

        if (evt.type == EventType.DragPerform && draggedObjects.Length > 0)
        {
            DragAndDrop.AcceptDrag();
            bool added = false;
            foreach (var obj in draggedObjects)
            {
                var component = obj as Component;
                var go = component != null ? component.gameObject : (GameObject)obj;
                string componentType = component != null ? component.GetType().Name : UGUIScriptGeneratorContext.GetDefaultComponentType(go);
                if (uiElements.Any(element => element != null && element.uiObject == go && element.selectedComponentType == componentType))
                    continue;

                var elementToAdd = new UIElement("", go) { selectedComponentType = componentType };
                context.AutoUpdateFieldNameForNew(elementToAdd);
                uiElements.Add(elementToAdd);
                added = true;
            }
            if (added)
            {
                context.CheckRootNodeConsistency();
                PersistConfiguration();
            }
            Repaint();
        }
        evt.Use();
        return true;
    }
}
