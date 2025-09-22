using UnityEditor;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelDataReader;
using UnityEngine;

namespace ReadConf
{
    // ===== 编译完成后自动续跑（跨域重载） =====
    [InitializeOnLoad]
    static class ConfGenWorkflow
    {
        const string PendingKey = "ConfGen.Pending";
        const string SideKey    = "ConfGen.Side";

        static ConfGenWorkflow()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            // 没有待处理就退出
            if (!SessionState.GetBool(PendingKey, false)) return;

            // 编译尚未完成：显示进度条并等待
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayProgressBar("配置表", "等待脚本编译完成...", 0.6f);
                return;
            }

            // 编译完成：清理进度条，继续读表
            EditorUtility.ClearProgressBar();

            var side = SessionState.GetString(SideKey, "C");
            try
            {
                // 重新扫描（不再生成代码），拿到最新映射
                var content = GenCodeEditor.ScanOnly(side);
                ReadConfEditor.ReadConfData(content);
                EditorUtility.DisplayDialog("配置表生成", "代码生成 & 数据导入完成！", "确定");
            }
            catch (Exception ex)
            {
                Debug.LogError("[配置表] 后续读表失败：" + ex);
                EditorUtility.DisplayDialog("配置表生成", "后续读表失败，看 Console 日志。", "确定");
            }
            finally
            {
                SessionState.EraseBool(PendingKey);
                SessionState.EraseString(SideKey);
            }
        }

        public static void BeginWait(string side)
        {
            SessionState.SetBool(PendingKey, true);
            SessionState.SetString(SideKey, string.IsNullOrWhiteSpace(side) ? "C" : side.ToUpperInvariant());
            EditorUtility.DisplayProgressBar("配置表", "等待脚本编译完成...", 0.3f);
        }
    }

    public class ReadConfEditor
    {
        [MenuItem("Tool/配置表/配置表本地生成")]
        public static void ReadConfClient()
        {
            GenerateAndWait("C");
        }

        private static void GenerateAndWait(string side)
        {
            try
            {
                // 1) 生成代码 & Racast（按 .schema.json + side）
                GenCodeEditor.DoGenConfCode(side);
                Debug.Log($"[配置表] 代码生成成功（side={side}）");

                // 2) 标记“待继续”，刷新触发编译；编译完成后 ConfGenWorkflow.Tick 会继续 ReadConfData
                ConfGenWorkflow.BeginWait(side);
                AssetDatabase.Refresh(); // 触发编译 & 域重载
            }
            catch (Exception ex)
            {
                Debug.LogError("[配置表] 生成失败：" + ex);
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// 解析配置数据到 ConfData（仅在编译完成后调用）
        /// </summary>
        public static void ReadConfData(GenCodeEditor.GenCodeContent content)
        {
            ConfData data = new ConfData();
            var fields = typeof(ConfData).GetFields();

            foreach (var field in fields)
            {
                Type elemType   = field.FieldType.GetElementType();
                FieldInfo[] sub = elemType.GetFields();

                string className = char.ToUpper(field.Name[0]) + field.Name[1..];

                var cc = content.classes.FirstOrDefault(c => c.className == className);
                if (cc == null)
                {
                    Debug.LogWarning($"未找到匹配 ClassContent：{className}");
                    continue;
                }

                string filePath = cc.FilePath;
                try
                {
                    var headerNames = new List<string>();
                    var confList    = new List<object>();

                    if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        ReadCsvFile(filePath, elemType, sub, headerNames, confList);
                        var arr = Array.CreateInstance(elemType, confList.Count);
                        for (int i = 0; i < confList.Count; i++) arr.SetValue(confList[i], i);
                        field.SetValue(data, arr);
                    }
                    else if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                             filePath.EndsWith(".xls",  StringComparison.OrdinalIgnoreCase))
                    {
                        ReadExcelFile(filePath, content, elemType, sub, data, className);
                    }
                    else
                    {
                        Debug.LogWarning($"不支持的文件格式: {filePath}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"解析文件 {filePath} 出错: {e.Message}");
                }
            }

            FileBytesUtil.SaveConfDataToByteFile(data, ResEditorConfig.ConfsAsset_Path);
            Debug.Log("[配置表] 数据已序列化到 bytes");
        }

        // ========== CSV：只读第一行表头，其余全是数据 ==========
        private static void ReadCsvFile(string filePath, Type elemType, FieldInfo[] subFields,
            List<string> headerNames, List<object> confList)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                TrimOptions = TrimOptions.None,
                PrepareHeaderForMatch = args => args.Header.Trim(),
                BadDataFound = null
            };

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, config);

            if (!csv.Read() || !csv.ReadHeader())
            {
                Debug.LogWarning($"[CSV] 缺少表头：{filePath}");
                return;
            }
            headerNames.AddRange(csv.HeaderRecord.Select(ReadConfEditorUtil.ToCamelLower));

            // 直接开始读数据（不再有优先级/类型行）
            while (csv.Read())
            {
                var rowObj = Activator.CreateInstance(elemType);

                for (int i = 0; i < subFields.Length; i++)
                {
                    try
                    {
                        string name = subFields[i].Name;
                        int idx = headerNames.IndexOf(name);
                        if (idx < 0) continue;

                        string value = csv.GetField(idx);
                        object val = ConvertValue(value, subFields[i].FieldType);
                        subFields[i].SetValue(rowObj, val);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"CSV 解析错误: {ex.Message}");
                    }
                }
                confList.Add(rowObj);
            }
        }

        // ========== Excel：只读第一行表头，其余全是数据 ==========
        private static void ReadExcelFile(string filePath, GenCodeEditor.GenCodeContent content,
            Type elemType, FieldInfo[] subFields, ConfData data, string currentClassName)
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            do
            {
                string sheetName = reader.Name;
                if (ReadConfEditorUtil.ToCamelUpper(sheetName) != currentClassName)
                    continue;

                var field = typeof(ConfData).GetField(currentClassName,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (field == null)
                {
                    Debug.LogWarning($"ConfData 中未找到字段: {currentClassName}");
                    continue;
                }

                var headerNames = new List<string>();
                var confList    = new List<object>();
                bool gotHeader  = false;

                while (reader.Read())
                {
                    if (!gotHeader)
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                            headerNames.Add(ReadConfEditorUtil.ToCamelLower(reader.GetValue(i)?.ToString()?.Trim() ?? string.Empty));
                        gotHeader = true;
                        continue;
                    }

                    var rowObj = Activator.CreateInstance(elemType);
                    for (int i = 0; i < subFields.Length; i++)
                    {
                        try
                        {
                            string name = subFields[i].Name;
                            int idx = headerNames.IndexOf(name);
                            if (idx < 0) continue;

                            string? value = reader.IsDBNull(idx) ? null : reader.GetValue(idx)?.ToString();
                            object val = ConvertValue(value, subFields[i].FieldType);
                            subFields[i].SetValue(rowObj, val);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Excel 解析错误: {ex.Message}");
                        }
                    }
                    confList.Add(rowObj);
                }

                var arr = Array.CreateInstance(elemType, confList.Count);
                for (int i = 0; i < confList.Count; i++) arr.SetValue(confList[i], i);
                field.SetValue(data, arr);

            } while (reader.NextResult());
        }

        // ========== 类型转换 ==========
        private static object ConvertValue(string value, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(value)) return GetDefaultValue(targetType);

            try
            {
                if (targetType == typeof(int))       return int.TryParse(value, out var v) ? v : 0;
                if (targetType == typeof(long))      return long.TryParse(value, out var v) ? v : 0L;
                if (targetType == typeof(float))     return float.TryParse(value, out var v) ? v : 0f;
                if (targetType == typeof(double))    return double.TryParse(value, out var v) ? v : 0d;
                if (targetType == typeof(bool))      return bool.TryParse(value, out var v) && v;
                if (targetType == typeof(int[]))     return ParseIntArray(value);
                if (targetType == typeof(List<int>)) return ParseIntList(value);
            }
            catch (Exception ex)
            {
                Debug.LogError($"ConvertValue 错误: {ex.Message}");
            }
            return value;
        }
        private static object GetDefaultValue(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;

        private static int[] ParseIntArray(string value)
        {
            value = value.Trim();
            if (value.StartsWith("[") && value.EndsWith("]")) value = value[1..^1];
            return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out var v) ? v : 0).ToArray();
        }
        private static List<int> ParseIntList(string value)
        {
            value = value.Trim();
            if (value.StartsWith("[") && value.EndsWith("]")) value = value[1..^1];
            return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out var v) ? v : 0).ToList();
        }
    }
}
