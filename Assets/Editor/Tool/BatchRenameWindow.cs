using System.IO;
using UnityEditor;
using UnityEngine;

public class BatchRenameWindow : EditorWindow
{
    private string directoryPath = "Assets/";
    private string renameFormat = "img_pet_av_{0:D4}";
    private string logMessages = "";

    private const string DirectoryPathKey = "BatchRename_LastDirectory";
    private const string RenameFormatKey = "BatchRename_LastFormat";

    [MenuItem("Tool/批量重命名文件")]
    public static void ShowWindow()
    {
        var window = GetWindow<BatchRenameWindow>("Batch Rename Files");
        window.LoadPreferences();
    }

    private void LoadPreferences()
    {
        if (EditorPrefs.HasKey(DirectoryPathKey))
        {
            directoryPath = EditorPrefs.GetString(DirectoryPathKey);
        }
        if (EditorPrefs.HasKey(RenameFormatKey))
        {
            renameFormat = EditorPrefs.GetString(RenameFormatKey);
        }
    }

    private void SavePreferences()
    {
        EditorPrefs.SetString(DirectoryPathKey, directoryPath);
        EditorPrefs.SetString(RenameFormatKey, renameFormat);
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Rename Tool", EditorStyles.boldLabel);

        // 路径选择
        GUILayout.Label("Target Directory", EditorStyles.label);
        GUILayout.BeginHorizontal();
        EditorGUILayout.TextField(directoryPath, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Target Directory", directoryPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                directoryPath = selectedPath;
                SavePreferences();
            }
        }
        GUILayout.EndHorizontal();

        // 文件名格式
        GUILayout.Label("Rename Format", EditorStyles.label);
        renameFormat = EditorGUILayout.TextField(renameFormat);
        GUILayout.Label("Example: Use {0:D4} for 4-digit ID (e.g., img_pet_av_{0:D4}).");

        if (GUI.changed) // 当用户更改输入时自动保存
        {
            SavePreferences();
        }

        // 执行重命名
        if (GUILayout.Button("Rename Files"))
        {
            logMessages = RenameFilesInDirectory(directoryPath, renameFormat);
            AssetDatabase.Refresh();
        }

        // 显示日志
        GUILayout.Label("Log Output", EditorStyles.boldLabel);
        GUILayout.TextArea(logMessages, GUILayout.Height(200));
    }

    private string RenameFilesInDirectory(string directoryPath, string renameFormat)
    {
        if (!Directory.Exists(directoryPath))
        {
            return $"Error: Directory {directoryPath} does not exist.";
        }

        string[] files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
        var logBuilder = new System.Text.StringBuilder();

        foreach (string filePath in files)
        {
            string fileExtension = Path.GetExtension(filePath);

            // 跳过 .meta 文件
            if (fileExtension == ".meta")
            {
                logBuilder.AppendLine($"Skipped (meta file): {filePath}");
                continue;
            }

            string fileName = Path.GetFileNameWithoutExtension(filePath);

            // 提取 ID
            if (fileName.Contains("_"))
            {
                string[] parts = fileName.Split('_');
                foreach (string part in parts)
                {
                    if (int.TryParse(part, out int id))
                    {
                        // 使用格式化
                        string newFileName = string.Format(renameFormat, id) + fileExtension;
                        string directory = Path.GetDirectoryName(filePath);
                        string newFilePath = Path.Combine(directory, newFileName);

                        // 重命名文件
                        if (filePath != newFilePath)
                        {
                            File.Move(filePath, newFilePath);
                            logBuilder.AppendLine($"Renamed: {filePath} -> {newFilePath}");
                        }
                    }
                }
            }
            else
            {
                logBuilder.AppendLine($"Skipped (no ID found): {filePath}");
            }
        }

        logBuilder.AppendLine("Renaming completed!");
        return logBuilder.ToString();
    }
}