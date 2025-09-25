using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Text;

public static class ConfGenMenu
{
    [MenuItem("Tool/配置表/NuGet生成")]
    static void RunConfGen()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var bat  = Path.Combine(root, "Tool", "ConfGen", "confgen.bat");

        if (!File.Exists(bat))
        {
            EditorUtility.DisplayDialog("配置表生成", $"未找到脚本：\n{bat}", "确定");
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("配置表生成", "正在生成代码与数据...", 0.5f);

            var (code, stdout, stderr) = RunProcess(
                "cmd.exe",
                $"/c \"\"{bat}\" --nopause\"",
                root
            );

            if (code != 0)
            {
                GameLogger.LogError($"ConfGen 失败：\n{stderr}\n{stdout}");
                EditorUtility.DisplayDialog("配置表生成失败",
                    $"错误码：{code}\n\n{(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr)}",
                    "确定");
            }
            else
            {
                GameLogger.Log($"ConfGen 成功：\n{stdout}");
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("配置表生成完成", "已成功生成并刷新资源。", "好的");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static (int exitCode, string stdout, string stderr) RunProcess(
        string fileName, string args, string? workingDir = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = fileName,
            Arguments              = args,
            WorkingDirectory       = workingDir ?? "",
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding  = Encoding.UTF8,
        };

        using var p = Process.Start(psi)!;
        string stdout = p.StandardOutput.ReadToEnd();
        string stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, stdout, stderr);
    }
}