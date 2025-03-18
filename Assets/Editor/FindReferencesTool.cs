using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class FindReferencesTool : EditorWindow
{
    private List<string> references = new List<string>();
    private string selectedObjectName;
    
    [MenuItem("Assets/工具/查找所有引用")]
    private static void FindAllReferences()
    {
        Object selectedObject = Selection.activeObject;

        if (selectedObject == null)
        {
            Debug.LogWarning("没有选中任何对象！");
            return;
        }

        FindReferencesTool window = GetWindow<FindReferencesTool>("引用查找结果");
        window.selectedObjectName = selectedObject.name;
        window.references.Clear();

        string path = AssetDatabase.GetAssetPath(selectedObject);
        string[] guids = AssetDatabase.FindAssets("t:Object");

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

            if (asset == selectedObject) continue;

            if (AssetDatabase.GetDependencies(assetPath, true).Contains(path))
            {
                window.references.Add(assetPath);
            }
        }

        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label($"引用了 {selectedObjectName} 的资源:", EditorStyles.boldLabel);
        if (references.Count == 0)
        {
            GUILayout.Label("没有找到引用。", EditorStyles.wordWrappedLabel);
        }
        else
        {
            foreach (var reference in references)
            {
                if (GUILayout.Button(reference, EditorStyles.label))
                {
                    SelectAsset(reference);
                }
            }
        }

        if (GUILayout.Button("关闭"))
        {
            Close();
        }
    }

    private void SelectAsset(string assetPath)
    {
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        if (asset != null)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
        else
        {
            Debug.LogWarning($"无法找到资源: {assetPath}");
        }
    }
}