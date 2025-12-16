using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UI;

public class UGUIScriptGenerator : EditorWindow
{
    // 存储 UI 元素的列表
    public List<UIElement> uiElements = new List<UIElement>();

    // 输出脚本路径
    public string scriptOutputPath = string.Empty;

    // 当前根节点
    private GameObject rootNode;
    private bool isRootNodeChecked = false;

    // TODO: 自定义命名空间名
    public static string NameSpaceName { get; private set; } = "Yuki";

    // 组件推荐优先级
    private readonly List<Type> componentPriority = new List<Type>
    {
        // TODO: 自定义类型
        typeof(UIList),
        typeof(UIListMulti),
        typeof(EnhancedScrollerController),
        typeof(Button),
        typeof(Toggle),
        typeof(Text),
        typeof(Slider),
        typeof(Image),
        typeof(TextMeshProUGUI)
    };

    // region 定位
    private const string CALLBACKS_REGION_TAG = "[控件回调](自动补全)";
    private const string CALLBACKS_REGION_WARN = "//按照region定位回调位置，禁止删除#region或修改";

    [MenuItem("Tool/UGUI Script Generator")]
    public static void ShowWindow()
    {
        GetWindow<UGUIScriptGenerator>("UGUI Script Generator");
    }

    /// <summary>
    /// 右键：从已挂载的 BasePanel 上读取 SerializeField（老控件：字段名锁定不变）
    /// </summary>
    [MenuItem("GameObject/工具/Generate UI Script for UIPanel", false, 10)]
    private static void GenerateUIScriptForSelectedPanel()
    {
        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null || !selectedObj.GetComponent<BasePanel>())
        {
            Debug.LogError("Please select a valid UIPanel that inherits from BasePanel.");
            return;
        }

        BasePanel basePanel = selectedObj.GetComponent<BasePanel>();
        if (basePanel == null)
        {
            Debug.LogError("Selected object does not have a BasePanel component.");
            return;
        }

        UGUIScriptGenerator window = GetWindow<UGUIScriptGenerator>("UGUI Script Generator");
        window.uiElements.Clear();

        FieldInfo[] fields = basePanel.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (!Attribute.IsDefined(field, typeof(SerializeField)) || !field.IsPrivate)
                continue;

            var fieldValue = field.GetValue(basePanel);
            if (fieldValue == null || fieldValue.Equals(null))
                continue;

            if (fieldValue is Component component)
            {
                var element = new UIElement(field.Name, component.gameObject)
                {
                    selectedComponentType = field.FieldType.Name,
                    isLocked = true
                };
                window.uiElements.Add(element);
            }
            else
            {
                var element = new UIElement(field.Name, fieldValue as GameObject)
                {
                    selectedComponentType = "GameObject",
                    isLocked = true
                };
                window.uiElements.Add(element);
            }
        }

        window.rootNode = selectedObj;
        window.isRootNodeChecked = true;
        window.Repaint();
    }

    // -------------------- GUI --------------------
    private void OnGUI()
    {
        bool hasError = false;
        List<int> elementsToRemove = new List<int>();

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
                        element.selectedComponentType = GetDefaultComponentType(element.uiObject);

                    // 新控件：字段名总是 name+type
                    AutoUpdateFieldNameForNew(element);
                    CheckRootNodeConsistency();
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
                        AutoUpdateFieldNameForNew(element);
                    }
                }

                GUI.enabled = prevEnabled;

                GUILayout.Label(element.isLocked ? "锁定" : "新", GUILayout.Width(28));
                GUILayout.Label($"字段: {element.fieldName}", GUILayout.MinWidth(240));
            }

            if (GUILayout.Button("移除", GUILayout.Width(60)))
                elementsToRemove.Add(i);

            GUILayout.EndHorizontal();

            if (IsDuplicateSameObjSameType(element))
            {
                EditorGUILayout.HelpBox($"!! 同一物体“{element.uiObject?.name}”同一类型“{element.selectedComponentType}”重复", MessageType.Error);
                hasError = true;
            }

            if (element.uiObject != null)
            {
                var others = GetSameNameOthers(element.uiObject);
                if (others.Count > 0)
                {
                    EditorGUILayout.HelpBox($"!! 存在不同物体同名：“{element.uiObject.name}”", MessageType.Error);
                    hasError = true;

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    if (GUILayout.Button("定位当前", GUILayout.Width(80)))
                        SelectAndPing(element.uiObject);

                    if (GUILayout.Button("定位同名", GUILayout.Width(80)))
                        SelectOtherSameName(element.uiObject);
                    GUILayout.EndHorizontal();
                }
            }
        }

        foreach (int index in elementsToRemove.OrderByDescending(i => i))
        {
            uiElements.RemoveAt(index);
            CheckRootNodeConsistency();
        }

        if (GUILayout.Button("添加UI元素"))
        {
            uiElements.Add(new UIElement("", null) { isLocked = false });
            isRootNodeChecked = false;
        }

        GUILayout.Label(rootNode != null ? $"当前根节点: {rootNode.name}" : "没有选中的根节点");

        if (GUILayout.Button("检查根节点一致性"))
        {
            CheckRootNodeConsistency();
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
            GenerateScript();
        }
        GUI.enabled = true;

        HandleDragAndDrop();
    }

    // -------------------- 核心逻辑 --------------------

    private void AutoUpdateFieldNameForNew(UIElement e)
    {
        if (e == null || e.uiObject == null) return;
        if (e.isLocked) return;

        if (string.IsNullOrEmpty(e.selectedComponentType))
            e.selectedComponentType = GetDefaultComponentType(e.uiObject);

        e.fieldName = $"{SanitizeIdentifier(e.uiObject.name)}_{e.selectedComponentType}";
    }

    private static string SanitizeIdentifier(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "Field";
        raw = Regex.Replace(raw, @"\s+", "");
        raw = Regex.Replace(raw, @"[^\w]", "_");
        if (char.IsDigit(raw[0])) raw = "_" + raw;
        return raw;
    }

    private string GetDefaultComponentType(GameObject go)
    {
        if (go == null) return "GameObject";

        var components = go.GetComponents<Component>();
        foreach (var type in componentPriority)
        {
            foreach (var component in components)
            {
                if (component == null) continue;
                var ct = component.GetType();
                if (type.IsAssignableFrom(ct))
                    return ct.Name;
            }
        }
        return "GameObject";
    }

    // 用“物体上真实组件类型”判断 Button/Toggle 派生（避免字符串解析歧义）
    private static Type GetSelectedComponentTypeFromObject(UIElement e)
    {
        if (e == null || e.uiObject == null || string.IsNullOrEmpty(e.selectedComponentType))
            return null;

        foreach (var c in e.uiObject.GetComponents<Component>())
        {
            if (c == null) continue;
            if (c.GetType().Name == e.selectedComponentType)
                return c.GetType();
        }

        // Transform 也是 Component，但不会出现在 GetComponents<Component>() 里？会的
        if (e.selectedComponentType == "Transform") return typeof(Transform);
        if (e.selectedComponentType == "GameObject") return typeof(GameObject);

        return null;
    }

    private static bool IsButtonLike(UIElement e)
    {
        var t = GetSelectedComponentTypeFromObject(e);
        return t != null && typeof(Button).IsAssignableFrom(t);
    }

    private static bool IsToggleLike(UIElement e)
    {
        var t = GetSelectedComponentTypeFromObject(e);
        return t != null && typeof(Toggle).IsAssignableFrom(t);
    }

    // -------------------- 冲突校验（生成前） --------------------

    private enum ConflictType
    {
        DuplicateNameDifferentObject,
        DuplicateSameObjectSameType,
        FieldNameConflict
    }

    private class ConflictInfo
    {
        public ConflictType type;
        public string title;
        public string message;
        public GameObject oldObj;
        public GameObject newObj;
    }

    private void GenerateScript()
    {
        if (string.IsNullOrEmpty(scriptOutputPath))
        {
            Debug.LogError("Please provide a valid script output path.");
            return;
        }

        if (rootNode == null)
        {
            Debug.LogError("Please assign a consistent root node (ending with 'UIPanel').");
            return;
        }

        // 刷新新控件字段名
        foreach (var e in uiElements)
            AutoUpdateFieldNameForNew(e);

        // 生成前：精准冲突校验
        var conflict = ValidateConflicts();
        if (conflict != null)
        {
            ShowConflictDialog(conflict);
            return;
        }

        // 锁定
        foreach (var e in uiElements)
        {
            if (e != null && e.uiObject != null)
                e.isLocked = true;
        }

        // 再次确保 root 一致
        foreach (var element in uiElements)
        {
            if (element == null || element.uiObject == null) continue;
            GameObject elementRoot = FindRootNode(element.uiObject);
            if (elementRoot != rootNode)
            {
                Debug.LogError($"UI Element {element.uiObject.name} has a different root node.");
                return;
            }
        }

        string className = rootNode.name;

        string classFolderPath = Path.Combine(scriptOutputPath, className);
        Directory.CreateDirectory(classFolderPath);

        // View.cs 每次覆盖
        string viewScriptContent = GenerateViewScriptContent(className, uiElements);
        string viewFilePath = Path.Combine(classFolderPath, className + ".View.cs");
        File.WriteAllText(viewFilePath, viewScriptContent);

        // Panel.cs 只首次生成
        string classFilePath = Path.Combine(classFolderPath, className + ".cs");
        if (!File.Exists(classFilePath))
        {
            string panelScriptContent = GeneratePanelScriptContent(className);
            File.WriteAllText(classFilePath, panelScriptContent);
        }

        // Bind.cs：自动绑定区重写；回调区保留并追加 stub
        string bindFilePath = Path.Combine(classFolderPath, className + ".Bind.cs");
        UpdateBindFile(bindFilePath, className, uiElements, classFilePath);

        // 关键：把“待挂载/待赋值”写到 SessionState，等待编译结束后自动执行
        UGUIScriptGeneratorPendingApplier.SetPending(rootNode, $"{NameSpaceName}.{className}", uiElements);

        AssetDatabase.Refresh();

        Debug.Log($"Scripts generated at: {classFolderPath} (will auto-attach after compile)");
    }

    private ConflictInfo ValidateConflicts()
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

    private void ShowConflictDialog(ConflictInfo conflict)
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

    private bool IsDuplicateSameObjSameType(UIElement element)
    {
        if (element == null || element.uiObject == null) return false;
        return uiElements.Count(e =>
            e != null &&
            e.uiObject == element.uiObject &&
            e.selectedComponentType == element.selectedComponentType
        ) > 1;
    }

    // -------------------- Bind.cs：按 region 保留回调区 + 追加 stub --------------------

    private void UpdateBindFile(string bindFilePath, string className, List<UIElement> elements, string classFilePath)
    {
        string panelText = File.Exists(classFilePath) ? File.ReadAllText(classFilePath) : "";
        string existing = File.Exists(bindFilePath) ? File.ReadAllText(bindFilePath) : "";

        string preservedCallbacks = ExtractCallbacksBlock(existing);
        string mergedCallbacks = AppendMissingCallbackStubs(preservedCallbacks, panelText, elements);

        string newText = GenerateBindFileFullText(className, elements, mergedCallbacks);
        File.WriteAllText(bindFilePath, newText);
    }

    private string ExtractCallbacksBlock(string bindText)
    {
        if (string.IsNullOrEmpty(bindText)) return "";

        // 只认：#region[控件回调](自动补全) ... #endregion（此处 body 是 region 内全部内容）
        string pattern =
            @"^[ \t]*#region\s*" + Regex.Escape(CALLBACKS_REGION_TAG) + @"\s*(?:\r\n|\n|\r)" +
            @"(?<body>[\s\S]*?)" +
            @"^[ \t]*#endregion\s*$";

        var m = Regex.Match(bindText, pattern, RegexOptions.Multiline);
        if (!m.Success) return "";

        string body = m.Groups["body"].Value;

        // 抽取时去掉警告行（只去一次）
        var warnRegex = new Regex(
            @"^[ \t]*" + Regex.Escape(CALLBACKS_REGION_WARN) + @"[ \t]*(?:\r\n|\n|\r)",
            RegexOptions.Multiline
        );
        body = warnRegex.Replace(body, "", 1, 0);
        // 去掉回调区开头/结尾的空行（避免每次生成都越堆越多）
        body = body.Trim('\r', '\n');
        if (body.Length > 0) body += "\n";


        return body;
    }

    private string AppendMissingCallbackStubs(string callbacksBlock, string panelText, List<UIElement> elements)
    {
        string merged = callbacksBlock ?? "";

        foreach (var e in elements)
        {
            if (e == null || e.uiObject == null) continue;

            bool isBtn = IsButtonLike(e);
            bool isTgl = IsToggleLike(e);
            if (!isBtn && !isTgl) continue;

            string methodName = $"{e.fieldName}Callback";

            if (ContainsMethod(panelText, methodName) || ContainsMethod(merged, methodName))
                continue;

            if (merged.Length > 0 && !merged.EndsWith("\n")) merged += "\n";
            if (isBtn)
            {
                merged += $"        void {methodName}()\n";
                merged += "        {\n";
                merged += "            // TODO: Add your logic here\n";
                merged += "        }\n";
            }
            else
            {
                merged += $"        void {methodName}(bool isOn)\n";
                merged += "        {\n";
                merged += "            // TODO: Add your logic here\n";
                merged += "        }\n";
            }
        }

        return merged;
    }

    private bool ContainsMethod(string text, string methodName)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(methodName)) return false;
        return Regex.IsMatch(text, $@"\b{Regex.Escape(methodName)}\b");
    }

    private string GenerateBindFileFullText(string className, List<UIElement> elements, string callbacksBlock)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// 说明：");
        sb.AppendLine("// 1) #region 自动绑定（生成器维护）会被重写，请不要手改。");
        sb.AppendLine("// 2) #region[控件回调](自动补全) 内可编辑，生成器只会追加缺失的回调方法。");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine();
        sb.AppendLine($"namespace {NameSpaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    public partial class {className}");
        sb.AppendLine("    {");

        sb.AppendLine("        #region 自动绑定（生成器维护）");
        sb.AppendLine("        private bool _uiEventsBound;");
        sb.AppendLine();
        sb.AppendLine("        private void BindUIEvents()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_uiEventsBound) return;");
        sb.AppendLine("            _uiEventsBound = true;");
        sb.AppendLine();

        foreach (var e in elements)
        {
            if (e == null || e.uiObject == null) continue;

            bool isBtn = IsButtonLike(e);
            bool isTgl = IsToggleLike(e);

            if (isBtn)
                sb.AppendLine($"            if ({e.fieldName} != null) {e.fieldName}.onClick.AddListener({e.fieldName}Callback);");
            if (isTgl)
                sb.AppendLine($"            if ({e.fieldName} != null) {e.fieldName}.onValueChanged.AddListener({e.fieldName}Callback);");
        }

        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        private void UnbindUIEvents()");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!_uiEventsBound) return;");
        sb.AppendLine("            _uiEventsBound = false;");
        sb.AppendLine();

        foreach (var e in elements)
        {
            if (e == null || e.uiObject == null) continue;

            bool isBtn = IsButtonLike(e);
            bool isTgl = IsToggleLike(e);

            if (isBtn)
                sb.AppendLine($"            if ({e.fieldName} != null) {e.fieldName}.onClick.RemoveListener({e.fieldName}Callback);");
            if (isTgl)
                sb.AppendLine($"            if ({e.fieldName} != null) {e.fieldName}.onValueChanged.RemoveListener({e.fieldName}Callback);");
        }

        sb.AppendLine("        }");
        sb.AppendLine("        #endregion");
        sb.AppendLine();

        sb.AppendLine($"        #region{CALLBACKS_REGION_TAG}");
        sb.AppendLine($"        {CALLBACKS_REGION_WARN}");
        sb.Append(callbacksBlock ?? "");
        if (!string.IsNullOrEmpty(callbacksBlock) && !callbacksBlock.EndsWith("\n")) sb.AppendLine();
        sb.AppendLine("        #endregion");

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    // -------------------- View.cs / Panel.cs --------------------

    private string GenerateViewScriptContent(string className, List<UIElement> elements)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("using TMPro;");
        sb.AppendLine();
        sb.AppendLine($"namespace {NameSpaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    public partial class {className}");
        sb.AppendLine("    {");

        foreach (var element in elements)
        {
            if (element == null || element.uiObject == null) continue;
            string componentType = element.GetComponentType();
            sb.AppendLine($"        [SerializeField] private {componentType} {element.fieldName};");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private string GeneratePanelScriptContent(string className)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine($"namespace {NameSpaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    public partial class {className} : BasePanel");
        sb.AppendLine("    {");
        sb.AppendLine("        #region 生命周期");
        sb.AppendLine("        public override void OnShow(BasePanelArg arg = null)");
        sb.AppendLine("        {");
        sb.AppendLine("            base.OnShow(arg);");
        sb.AppendLine("            BindUIEvents();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public override void OnClose()");
        sb.AppendLine("        {");
        sb.AppendLine("            UnbindUIEvents();");
        sb.AppendLine("            base.OnClose();");
        sb.AppendLine("        }");
        sb.AppendLine("        #endregion");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    // -------------------- Root / DragDrop --------------------

    private GameObject FindRootNode(GameObject uiElement)
    {
        if (uiElement == null) return null;

        Transform currentTransform = uiElement.transform;
        while (currentTransform.parent != null)
        {
            currentTransform = currentTransform.parent;
            if (currentTransform.name.EndsWith("UIPanel"))
                return currentTransform.gameObject;
        }
        return null;
    }

    private void CheckRootNodeConsistency()
    {
        if (uiElements.Count == 0)
        {
            Debug.LogWarning("No UI elements assigned.");
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
        Debug.Log($"All UI elements have the same root node: {rootNode.name}");
    }

    private void HandleDragAndDrop()
    {
        Event evt = Event.current;
        Rect dropArea = GUILayoutUtility.GetRect(0, 200, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "拖拽 UI 到这里", EditorStyles.helpBox);

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is GameObject go)
                        {
                            var element = new UIElement("", go) { isLocked = false };
                            element.selectedComponentType = GetDefaultComponentType(go);
                            AutoUpdateFieldNameForNew(element);
                            uiElements.Add(element);
                        }
                    }
                    CheckRootNodeConsistency();
                }
                Event.current.Use();
                break;
        }
    }

    private string GetHierarchyPath(GameObject go)
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

    private static void SelectAndPing(GameObject go)
    {
        if (go == null) return;

        Selection.activeObject = go;
        EditorGUIUtility.PingObject(go);

        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();
    }

    private List<GameObject> GetSameNameOthers(GameObject current)
    {
        if (current == null) return new List<GameObject>();
        string n = current.name;

        return uiElements
            .Where(e => e != null && e.uiObject != null && e.uiObject != current && e.uiObject.name == n)
            .Select(e => e.uiObject)
            .Distinct()
            .ToList();
    }

    private void SelectOtherSameName(GameObject current)
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

// -------------------- UIElement --------------------
[Serializable]
public class UIElement
{
    public string fieldName;
    public GameObject uiObject;
    public string selectedComponentType;
    public bool isLocked;

    public UIElement(string fieldName, GameObject uiObject)
    {
        this.fieldName = fieldName;
        this.uiObject = uiObject;
        selectedComponentType = string.Empty;
        isLocked = false;
    }

    public string[] GetAvailableComponentTypes()
    {
        if (uiObject == null)
            return Array.Empty<string>();

        return uiObject.GetComponents<Component>()
            .Where(c => c != null)
            .Select(c => c.GetType().Name)
            .Prepend("GameObject")
            .Prepend("Transform")
            .ToArray();
    }

    public string GetComponentType() => selectedComponentType;

    public void UpdateSelectedComponentType(string newType)
    {
        selectedComponentType = newType;
        // 字段名由生成器统一控制（新控件自动 name+type，老控件锁定不改）
    }
}

// -------------------- 编译后自动挂载/自动赋值（关键修复点） --------------------
[InitializeOnLoad]
internal static class UGUIScriptGeneratorPendingApplier
{
    private const string KEY = "UGUI_SCRIPTGEN_PENDING_JSON";

    [Serializable]
    private class PendingItem
    {
        public string fieldName;
        public string targetGid; // GameObject GlobalObjectId
    }

    [Serializable]
    private class PendingPayload
    {
        public string rootGid;        // root GameObject GlobalObjectId
        public string fullClassName;  // e.g. Yuki.AUIPanel
        public List<PendingItem> items = new List<PendingItem>();
        public int retry;
    }

    static UGUIScriptGeneratorPendingApplier()
    {
        // 域重载后会执行这里，确保能把生成的脚本挂回去
        EditorApplication.delayCall += TryApplyPending;
        CompilationPipeline.compilationFinished += _ => EditorApplication.delayCall += TryApplyPending;
    }

    public static void SetPending(GameObject root, string fullClassName, List<UIElement> elements)
    {
        if (root == null) return;

        var payload = new PendingPayload
        {
            rootGid = GlobalObjectId.GetGlobalObjectIdSlow(root).ToString(),
            fullClassName = fullClassName,
            retry = 0
        };

        foreach (var e in elements)
        {
            if (e == null || e.uiObject == null) continue;

            payload.items.Add(new PendingItem
            {
                fieldName = e.fieldName,
                targetGid = GlobalObjectId.GetGlobalObjectIdSlow(e.uiObject).ToString()
            });
        }

        SessionState.SetString(KEY, JsonUtility.ToJson(payload));
    }

    private static void TryApplyPending()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { Reschedule(); return; }

        string json = SessionState.GetString(KEY, "");
        if (string.IsNullOrEmpty(json)) return;

        var payload = JsonUtility.FromJson<PendingPayload>(json);
        if (payload == null) { SessionState.EraseString(KEY); return; }

        var rootObj = ObjectFromGid(payload.rootGid) as GameObject;
        if (rootObj == null)
        {
            Debug.LogWarning("[UGUI ScriptGen] Pending apply failed: root not found.");
            SessionState.EraseString(KEY);
            return;
        }

        var scriptType = FindType(payload.fullClassName);
        if (scriptType == null || !typeof(Component).IsAssignableFrom(scriptType))
        {
            if (payload.retry++ < 50)
            {
                SessionState.SetString(KEY, JsonUtility.ToJson(payload));
                Reschedule();
                return;
            }

            Debug.LogWarning($"[UGUI ScriptGen] Pending apply failed: type not found or not Component: {payload.fullClassName}");
            SessionState.EraseString(KEY);
            return;
        }

        try
        {
            var comp = rootObj.GetComponent(scriptType) ?? rootObj.AddComponent(scriptType);

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            Undo.RecordObject(comp, "Auto Assign UI Elements");
            EditorUtility.SetDirty(comp);

            foreach (var item in payload.items)
            {
                var field = scriptType.GetField(item.fieldName, flags);
                if (field == null) continue;

                var targetGo = ObjectFromGid(item.targetGid) as GameObject;
                if (targetGo == null) continue;

                var ft = field.FieldType;

                object value = null;
                if (ft == typeof(GameObject)) value = targetGo;
                else if (ft == typeof(Transform)) value = targetGo.transform;
                else if (typeof(Component).IsAssignableFrom(ft))
                    value = targetGo.GetComponent(ft);

                if (value != null)
                    field.SetValue(comp, value);
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(comp);

            if (PrefabUtility.IsPartOfPrefabInstance(rootObj))
                PrefabUtility.ApplyPrefabInstance(rootObj, InteractionMode.AutomatedAction);

            Debug.Log($"[UGUI ScriptGen] Auto-attached & assigned: {payload.fullClassName} -> {rootObj.name}");
            SessionState.EraseString(KEY);
        }
        catch (Exception ex)
        {
            Debug.LogError("[UGUI ScriptGen] Pending apply exception:\n" + ex);
            SessionState.EraseString(KEY);
        }
    }

    private static void Reschedule()
    {
        EditorApplication.delayCall += TryApplyPending;
    }

    private static UnityEngine.Object ObjectFromGid(string gidStr)
    {
        if (string.IsNullOrEmpty(gidStr)) return null;
        if (!GlobalObjectId.TryParse(gidStr, out var gid)) return null;
        return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
    }

    private static Type FindType(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(fullName);
            if (t != null) return t;
        }
        return null;
    }
}
