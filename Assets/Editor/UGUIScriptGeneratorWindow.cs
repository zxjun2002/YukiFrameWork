using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;

public class UGUIScriptGeneratorWindow : EditorWindow
{
    // 存储 UI 元素的列表
    public List<UIElement> uiElements = new List<UIElement>();

    // 输出脚本的路径
    public string scriptOutputPath = string.Empty;

    // 存储当前找到的根节点
    private GameObject rootNode;
    private bool isRootNodeChecked = false; // 用于控制是否已通过根节点检查
    
    // 优先级队列，按顺序配置需要检测的类型
    private readonly List<Type> componentPriority = new List<Type>
    {
        typeof(Button),
        typeof(Text),
        typeof(Slider),
        typeof(Image),
        typeof(TextMeshProUGUI)
    };
    
    // 根据优先级队列获取默认组件类型
    public string GetDefaultComponentType(GameObject go = null)
    {
        foreach (var type in componentPriority)
        {
            if (go.GetComponent(type) != null)
            {
                return type.Name;
            }
        }
        return "GameObject";
    }

    // 创建并显示窗口
    [MenuItem("Window/UGUI Script Generator")]
    public static void ShowWindow()
    {
        GetWindow<UGUIScriptGeneratorWindow>("UGUI Script Generator");
    }
    
    [MenuItem("GameObject/Generate UI Script for UIPanel", false, 10)]
    private static void GenerateUIScriptForSelectedPanel()
    {
        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null || !selectedObj.GetComponent<BasePanel>())
        {
            Debug.LogError("Please select a valid UIPanel that inherits from BasePanel.");
            return;
        }

        // 获取 BasePanel 或其派生类的组件
        BasePanel basePanel = selectedObj.GetComponent<BasePanel>();
        if (basePanel == null)
        {
            Debug.LogError("Selected object does not have a BasePanel component.");
            return;
        }

        // 获取到生成UI元素脚本窗口
        UGUIScriptGeneratorWindow window = GetWindow<UGUIScriptGeneratorWindow>("UGUI Script Generator");
    
        // 清空现有的 UI 元素
        window.uiElements.Clear();

        // 获取 BasePanel 类型的所有私有字段（通过 SerializeField 修饰）
        System.Reflection.FieldInfo[] fields = basePanel.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    
        // 遍历这些字段，检查是否为 UI 元素并已赋值
        foreach (var field in fields)
        {
            // 检查字段是否被标记为 SerializeField，并且字段是 private
            if (Attribute.IsDefined(field, typeof(SerializeField)) && field.IsPrivate)
            {
                // 获取字段的值（UI 元素）
                var fieldValue = field.GetValue(basePanel);
                if (fieldValue != null)
                {
                    if (fieldValue is Component component)
                    {
                        var element = new UIElement(field.Name, component.gameObject)
                        {
                            selectedComponentType = component.GetType().Name
                        };
                        // 将已赋值的 UI 元素添加到 uiElements 列表
                        window.uiElements.Add(element); 
                    }
                    else
                    {
                        var element = new UIElement(field.Name, fieldValue as GameObject)
                        {
                            selectedComponentType = "GameObject"
                        };
                        // 将已赋值的 UI 元素添加到 uiElements 列表
                        window.uiElements.Add(element);
                    }
                }
            }
        }

        // 设置根节点
        window.rootNode = selectedObj;
        window.isRootNodeChecked = true;
        window.Repaint();  // 刷新窗口以显示新加载的 UI 元素
    }
    
    // GUI 绘制
    private void OnGUI()
    {
        // 记录需要删除的元素
        List<int> elementsToRemove = new List<int>();

        // 允许用户拖拽 UI 元素
        for (int i = 0; i < uiElements.Count; i++)
        {
            GUILayout.BeginHorizontal();
            var newUiObject = (GameObject)EditorGUILayout.ObjectField(uiElements[i].uiObject, typeof(GameObject), true);
            var element = uiElements[i];

            if (newUiObject != uiElements[i].uiObject) // 如果 UI 元素发生变化
            {
                // 检查是否已经存在该 UI 元素,并且不是重复类型
                if (uiElements.Any(e => e.uiObject == newUiObject && e.selectedComponentType == GetDefaultComponentType(newUiObject)))
                {
                    Debug.LogWarning($"UI element {newUiObject.name} already exists in the list, skipping.");
                    element.uiObject = null; // 置为空
                }
                else
                {
                    // 如果没有重复，则进行赋值
                    element.uiObject = newUiObject;

                    // 仅当 UI 元素不为空时才进行一致性检查
                    if (element.uiObject != null)
                    {
                        element.selectedComponentType = GetDefaultComponentType(element.uiObject);
                        element.fieldName = newUiObject.name + "_" + element.selectedComponentType;
                        CheckRootNodeConsistency(); // 自动检查一致性
                    }
                }
            }
            
            // 类型选择下拉框
            if (element.uiObject != null)
            {
                string[] availableTypes = element.GetAvailableComponentTypes();
                int currentIndex = Array.IndexOf(availableTypes, element.selectedComponentType);
                if (currentIndex == -1) currentIndex = 0;
                GUILayout.Label("类型", GUILayout.Width(30)); // 缩小宽度
                int newIndex = EditorGUILayout.Popup(currentIndex, availableTypes);
                if (newIndex != currentIndex)
                {
                    element.UpdateSelectedComponentType(availableTypes[newIndex]);
                }
            }

            // 删除按钮
            if (GUILayout.Button("移除", GUILayout.Width(60)))
            {
                elementsToRemove.Add(i); // 记录删除的元素索引
            }
            GUILayout.EndHorizontal();
        }
        
        // 删除所有需要删除的元素
        foreach (int index in elementsToRemove.OrderByDescending(i => i)) // 倒序删除避免索引问题
        {
            uiElements.RemoveAt(index);
            CheckRootNodeConsistency();
        }

        // 按钮来添加新的 UI 元素
        if (GUILayout.Button("添加UI元素"))
        {
            uiElements.Add(new UIElement("", null));
            isRootNodeChecked = false;
        }

        // 根节点显示和验证
        if (rootNode != null)
        {
            GUILayout.Label($"当前根节点: {rootNode.name}");
        }
        else
        {
            GUILayout.Label("没有选中的根节点");
        }

        // 检查根节点一致性
        if (GUILayout.Button("检查根节点一致性"))
        {
            CheckRootNodeConsistency();
        }

        // 生成脚本的按钮，只有在根节点一致性检查通过后才可点击
        GUI.enabled = isRootNodeChecked;  // 如果根节点一致性检查未通过，生成按钮不可点击
        if (GUILayout.Button("生成脚本(如果没有生成或挂载成功就再按一次)"))
        {
            GenerateScript();
        }
        GUI.enabled = true; // 恢复默认按钮状态
        
        // 获取之前保存的路径，防止每次都被重置
        string previousPath = EditorPrefs.GetString("ScriptOutputPath", "Assets/Scripts/UI/");
        if (string.IsNullOrEmpty(scriptOutputPath))
        {
            scriptOutputPath = previousPath;  // 如果路径为空，使用之前保存的路径
        }

        // 输出路径
        GUILayout.BeginHorizontal();
        scriptOutputPath = EditorGUILayout.TextField("脚本输出路径", scriptOutputPath);
        if (GUILayout.Button("选择路径", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Output Folder", scriptOutputPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                scriptOutputPath = "Assets" + path.Substring(Application.dataPath.Length);  // 转换为相对路径
                // 保存新的路径
                EditorPrefs.SetString("ScriptOutputPath", scriptOutputPath);
            }
        }
        GUILayout.EndHorizontal();
        // 检查拖放事件
        HandleDragAndDrop();
    }

    // 生成脚本
    private void GenerateScript()
    {
        // 确保路径有效
        if (string.IsNullOrEmpty(scriptOutputPath))
        {
            Debug.LogError("Please provide a valid script output path.");
            return;
        }

        // 确保所有 UI 元素的根节点一致
        if (rootNode == null)
        {
            Debug.LogError("Please assign a consistent root node (ending with 'UIPanel').");
            return;
        }

        foreach (var element in uiElements)
        {
            GameObject elementRoot = FindRootNode(element.uiObject);
            if (elementRoot != rootNode)
            {
                Debug.LogError($"UI Element {element.uiObject.name} has a different root node.");
                return;
            }
        }

        // 获取 UI 脚本类的名称（以根节点为基础）
        string className = rootNode.name; // 根节点名称作为类名

        // 生成脚本内容
        string scriptContent = GenerateScriptContent(className, uiElements);

        // 保存文件路径
        string filePath = Path.Combine(scriptOutputPath, className + ".cs");

        // 创建目录（如果不存在）
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

        // 写入文件
        File.WriteAllText(filePath, scriptContent);

        // 刷新资源数据库
        AssetDatabase.Refresh();

        // 通过反射加载生成的脚本
        MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(filePath);

        // 确保脚本挂载到根节点
        if (script != null)
        {
            // 判断是否已经挂载此脚本
            var existingComponent = rootNode.GetComponent(script.GetClass());
            if (existingComponent == null)
            {
                rootNode.AddComponent(script.GetClass());
            }

            // 获取添加的组件
            var component = rootNode.GetComponent(script.GetClass());

            // 为每个 UI 元素赋值
            AssignUIElements(component);
        }
        
        // 确保在脚本中正确应用和保存预制体
        if (PrefabUtility.IsPartOfPrefabInstance(rootNode))
        {
            // 这是场景中的预制体实例
            PrefabUtility.ApplyPrefabInstance(rootNode, InteractionMode.UserAction);
            Debug.Log($"Prefab applied: {rootNode.name}");
        }
        else
        {
            SavePrefabByName(rootNode);
        }

        Debug.Log($"Script generated at: {filePath}");
    }

    // 为生成的脚本中的变量赋值
    private void AssignUIElements(Component component)
    {
        foreach (var element in uiElements)
        {
            string fieldName = element.fieldName;
            string componentType = element.GetComponentType();

            // 获取字段时，确保包括私有字段
            var field = component.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                // 通过反射动态获取组件
                var componentInstance = element.uiObject.GetComponent(componentType);

                if (componentInstance != null)
                {
                    // 如果组件存在，设置字段为对应组件
                    field.SetValue(component, componentInstance);
                }
                else
                {
                    // 如果类型不匹配或没有组件，默认为 GameObject
                    field.SetValue(component, element.uiObject);
                }
            }
        }
    }
    //根据名字去查找预制体并保存
    private void SavePrefabByName(GameObject rootNode)
    {
        // 获取 rootNode 的名称
        string prefabName = rootNode.name;

        // 使用 AssetDatabase 查找同名预制体
        string[] prefabPaths = AssetDatabase.FindAssets(prefabName + " t:Prefab")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .ToArray();

        if (prefabPaths.Length > 0)
        {
            // 如果找到了同名预制体
            string prefabPath = prefabPaths[0];  // 获取第一个匹配的预制体路径
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            // 如果找到了该预制体，保存它
            PrefabUtility.SaveAsPrefabAsset(rootNode, prefabPath);
            Debug.Log($"Prefab saved at: {prefabPath}");
        }
        else
        {
            // 检查是否存在文件夹 'Assets/Prefabs/'
            string prefabFolderPath = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabFolderPath))
            {
                // 如果文件夹不存在，则创建文件夹
                AssetDatabase.CreateFolder("Assets", "Prefabs");
                Debug.Log($"Created folder: {prefabFolderPath}");
            }
            // 创建新预制体
            string newPrefabPath = prefabFolderPath + "/" + prefabName + ".prefab";
            PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(rootNode);
            // 只在该物体未作为预制体实例时创建新的预制体
            if (prefabAssetType == PrefabAssetType.NotAPrefab)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(rootNode, newPrefabPath, InteractionMode.UserAction);
                Debug.Log($"New prefab created at: {newPrefabPath}");
            }
            else
            {
                Debug.LogWarning("Prefab not found at path: " + newPrefabPath);
            }
        }
    }

    // 生成脚本的内容
    private string GenerateScriptContent(string className, List<UIElement> elements)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("using TMPro;"); // 如果你需要 TextMeshPro 的支持
        sb.AppendLine();

        sb.AppendLine($"public partial class {className} : BasePanel");
        sb.AppendLine("{");

        // 为每个 UI 元素生成字段
        foreach (var element in elements)
        {
            string componentType = element.GetComponentType();
            sb.AppendLine($"    [SerializeField] private {componentType} {element.fieldName};");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    // 查找给定 UI 元素的根节点
    private GameObject FindRootNode(GameObject uiElement)
    {
        if (uiElement == null)
        {
            return null;
        }
        Transform currentTransform = uiElement.transform;
        while (currentTransform.parent != null)
        {
            currentTransform = currentTransform.parent;
            if (currentTransform.name.EndsWith("UIPanel"))
            {
                return currentTransform.gameObject;
            }
        }
        return null; // 没有找到符合条件的根节点
    }

    // 检查根节点一致性
    private void CheckRootNodeConsistency()
    {
        if (uiElements.Count == 0)
        {
            Debug.LogWarning("No UI elements assigned.");
            return;
        }

        GameObject initialRoot = null;
        foreach (var variElement in uiElements)
        {
            if (variElement.uiObject != null)
            {
                initialRoot = FindRootNode(variElement.uiObject);
                break;
            }
        }
        if (initialRoot == null)
        {
            Debug.LogError("未找到当前UI元素符合条件的根节点");
            return;
        }

        rootNode = initialRoot;

        // 确保所有 UI 元素的根节点一致
        foreach (var element in uiElements)
        {
            GameObject elementRoot = FindRootNode(element.uiObject);
            if (elementRoot != rootNode)
            {
                isRootNodeChecked = false;
                if (elementRoot != null)
                {
                    Debug.LogError($"UI Element {element.uiObject.name} 有不同的节点");
                }
                else
                {
                    Debug.LogError("存在未赋值的UI元素");
                }
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
                            if (uiElements.Any(e => e.uiObject == go && e.selectedComponentType == GetDefaultComponentType(go)))
                            {
                                Debug.LogWarning($"UI element {go.name} already exists in the list, skipping.");
                            }
                            else
                            {
                                var element = new UIElement(go.name, go);
                                element.selectedComponentType = GetDefaultComponentType(element.uiObject);
                                uiElements.Add(element);
                            }
                        }
                    }
                    CheckRootNodeConsistency();
                }
                Event.current.Use();
                break;
        }
    }

}

// UIElement 扩展：支持用户选择的类型
[System.Serializable]
public class UIElement
{
    public string fieldName;
    public GameObject uiObject;
    public string selectedComponentType; // 用户选择的类型

    public UIElement(string fieldName, GameObject uiObject)
    {
        this.fieldName = fieldName;
        this.uiObject = uiObject;
        selectedComponentType = String.Empty; // 默认类型
    }

    // 获取 UI 元素挂载的所有组件类型
    public string[] GetAvailableComponentTypes()
    {
        if (uiObject == null)
            return Array.Empty<string>();

        return uiObject.GetComponents<Component>()
            .Select(c => c.GetType().Name).Prepend("GameObject")
            .ToArray();
    }
    //根据优先级队列获取组件名字
    public string GetComponentType()
    {
        return selectedComponentType;
    }

    // 更新当前选中的组件类型
    public void UpdateSelectedComponentType(string newType)
    {
        selectedComponentType = newType;
        fieldName = uiObject.name + "_" + selectedComponentType;
    }
}