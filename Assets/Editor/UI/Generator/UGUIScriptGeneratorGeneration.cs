using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal sealed partial class UGUIScriptGeneratorContext
{
    internal bool GenerateScript()
    {
        try { return GenerateScripts(); }
        catch (Exception exception) { Debug.LogError($"[UGUI ScriptGen] 生成失败：{exception.Message}"); return false; }
    }

    internal bool GenerateScripts()
    {
        if (string.IsNullOrEmpty(scriptOutputPath))
        {
            Debug.LogError("Please provide a valid script output path.");
            return false;
        }

        if (rootNode == null)
        {
            Debug.LogError("Please assign a consistent root node (ending with 'UIPanel').");
            return false;
        }

        RestoreBindings();
        if (uiElements.Any(element => element != null && element.isLocked && element.uiObject == null))
            throw new InvalidOperationException("存在未找到对象的 UI 引用，请在 Inspector 中重新赋值或移除后再生成。");
        // 刷新新控件字段名
        foreach (var e in uiElements)
            AutoUpdateFieldNameForNew(e);

        // 生成前：精准冲突校验
        var conflict = ValidateConflicts();
        if (conflict != null)
        {
            ShowConflictDialog(conflict);
            return false;
        }

        // 再次确保 root 一致
        foreach (var element in uiElements)
        {
            if (element == null || element.uiObject == null) continue;
            GameObject elementRoot = FindRootNode(element.uiObject);
            if (elementRoot != rootNode)
            {
                Debug.LogError($"UI Element {element.uiObject.name} has a different root node.");
                return false;
            }
        }

        var panel = rootNode.GetComponent<BasePanel>();
        string className = panel != null ? panel.GetType().Name : rootNode.name;
        if (panel != null) NameSpaceName = panel.GetType().Namespace ?? string.Empty;
        if (SanitizeIdentifier(className) != className)
            throw new InvalidOperationException("面板名称必须是有效的 C# 类名。");

        string classFolderPath = Path.Combine(scriptOutputPath, className);
        Directory.CreateDirectory(classFolderPath);

        string bindFilePath = Path.Combine(classFolderPath, className + ".Bind.cs");
        // 先完成旧文件迁移及增量补全，确认业务代码能被保留后再写文件。
        var eventScripts = BuildEventScriptContents(bindFilePath, className, uiElements);
        string viewScriptContent = GenerateViewScriptContent(className, uiElements);
        SaveBindings(classFolderPath, string.IsNullOrEmpty(NameSpaceName) ? className : $"{NameSpaceName}.{className}");

        // View.cs 每次覆盖
        string viewFilePath = Path.Combine(classFolderPath, className + ".View.cs");
        WriteScriptIfChanged(viewFilePath, viewScriptContent);

        // 先保存主业务文件，再替换旧 Bind，避免先覆盖旧业务代码。
        foreach (var script in eventScripts.OrderBy(script => script.Key == bindFilePath ? 1 : 0))
            WriteScriptIfChanged(script.Key, script.Value);

        foreach (var element in uiElements.Where(e => e.uiObject != null)) element.isLocked = true;

        // 关键：把“待挂载/待赋值”写到 SessionState，等待编译结束后自动执行
        UGUIScriptGeneratorPendingApplier.SetPending(rootNode,
            string.IsNullOrEmpty(NameSpaceName) ? className : $"{NameSpaceName}.{className}", uiElements);

        bindings.needsGeneration = false;
        UGUIBindingAccess.Save(bindings, false);

        AssetDatabase.Refresh();

        Debug.Log($"Scripts generated at: {classFolderPath} (will auto-attach after compile)");
        return true;
    }

    internal static void WriteScriptIfChanged(string path, string content)
    {
        if (!File.Exists(path) || File.ReadAllText(path) != content) File.WriteAllText(path, content);
    }
}
