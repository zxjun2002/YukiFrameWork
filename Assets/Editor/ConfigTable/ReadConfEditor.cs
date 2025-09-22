using UnityEditor;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelDataReader;
using UnityEngine;
using Newtonsoft.Json; // 用于跨域保存/恢复 GenCodeContent

namespace ReadConf
{
    // ===== 编译完成后自动续跑（跨域重载） =====
    [InitializeOnLoad]
    static class ConfGenWorkflow
    {
        const string PendingKey      = "ConfGen.Pending";
        const string SideKey         = "ConfGen.Side";
        const string ContentPathKey  = "ConfGen.ContentPath";

        static ConfGenWorkflow()
        {
            EditorApplication.update += Tick;
        }

        static string GetTempJsonPath()
        {
            // 放到 Library 里，避免进版本控制
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, "Library", "ConfGen.LastContent.json");
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
            
            var jsonPath = SessionState.GetString(ContentPathKey, GetTempJsonPath());
            try
            {
                if (!File.Exists(jsonPath))
                    throw new FileNotFoundException("未找到缓存的 GenCodeContent JSON", jsonPath);

                var json = File.ReadAllText(jsonPath, Encoding.UTF8);
                var content = JsonConvert.DeserializeObject<GenCodeEditor.GenCodeContent>(json);

                if (content == null || content.classes == null || content.classes.Count == 0)
                    throw new Exception("缓存的 GenCodeContent 为空或无表。");

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
                // 清理
                try
                {
                    if (File.Exists(jsonPath)) File.Delete(jsonPath);
                }
                catch { /* ignore */ }

                SessionState.EraseBool(PendingKey);
                SessionState.EraseString(SideKey);
                SessionState.EraseString(ContentPathKey);
            }
        }

        public static void BeginWait(string side, GenCodeEditor.GenCodeContent content)
        {
            // 将 content 序列化到临时 JSON，跨域传递
            var jsonPath = GetTempJsonPath();
            Directory.CreateDirectory(Path.GetDirectoryName(jsonPath)!);
            var json = JsonConvert.SerializeObject(content);
            File.WriteAllText(jsonPath, json, Encoding.UTF8);

            SessionState.SetBool(PendingKey, true);
            SessionState.SetString(SideKey, string.IsNullOrWhiteSpace(side) ? "C" : side.ToUpperInvariant());
            SessionState.SetString(ContentPathKey, jsonPath);

            EditorUtility.DisplayProgressBar("配置表", "等待脚本编译完成...", 0.3f);
        }
    }

    public class ReadConfEditor
    {
        [MenuItem("Tool/配置表/配置表本地生成")]
        public static void ReadConfClient() => GenerateAndWait("C");

        private static void GenerateAndWait(string side)
        {
            try
            {
                // 1) 生成代码 & Racast（按 .schema.json + side），拿到 content
                var content = GenCodeEditor.DoGenConfCode(side);
                Debug.Log($"[配置表] 代码生成成功（side={side}，表数={content?.classes?.Count ?? 0}）");

                // 2) 标记“待继续”，缓存 content，刷新触发编译；编译完成后 ConfGenWorkflow.Tick 会继续 ReadConfData
                ConfGenWorkflow.BeginWait(side, content);
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
            // 运行时查 ConfData 类型；没有就温和退出（不报编译错）
            var confDataType = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(t => t.Name == "ConfData");

            if (confDataType == null)
            {
                Debug.LogWarning("[配置表] 未找到 ConfData 类型，已跳过数据导入。（这通常表示刚生成代码但尚未编译完成）");
                return;
            }

            // new ConfData()
            var data = Activator.CreateInstance(confDataType)!;

            // 反射遍历字段：public T[] field;
            var fields = confDataType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var elemType  = field.FieldType.GetElementType();
                if (elemType == null) continue;

                var subFields = elemType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                string className = char.ToUpper(field.Name[0]) + field.Name.Substring(1);

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
                        ReadCsvFile(filePath, elemType, subFields, headerNames, confList);

                        var arr = Array.CreateInstance(elemType, confList.Count);
                        for (int i = 0; i < confList.Count; i++) arr.SetValue(confList[i], i);
                        field.SetValue(data, arr);
                    }
                    else if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                             filePath.EndsWith(".xls",  StringComparison.OrdinalIgnoreCase))
                    {
                        // 这里用传入的 FieldInfo 直接赋值
                        ReadExcelFile(filePath, elemType, subFields, data, className, field);
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

            // ---- 直接用 MemoryPack 反射序列化并写文件（不依赖 SaveConfDataToByteFile）----
            string outPath = Path.Combine(Application.dataPath, ResEditorConfig.ConfsAsset_Path);

            try
            {
                byte[] bytes;

                // 优先用非泛型 Serialize(Type, object, options)
                var nonGeneric = typeof(MemoryPack.MemoryPackSerializer)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                        m.Name == "Serialize" &&
                        !m.IsGenericMethod &&
                        m.GetParameters().Length >= 2 &&
                        m.GetParameters()[0].ParameterType == typeof(Type));

                if (nonGeneric != null)
                {
                    bytes = (byte[])nonGeneric.Invoke(null, new object?[] { confDataType, data, null });
                }
                else
                {
                    // 兜底：泛型 Serialize<T>(T, options)
                    var generic = typeof(MemoryPack.MemoryPackSerializer)
                        .GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .First(m => m.Name == "Serialize" && m.IsGenericMethodDefinition);

                    var gm = generic.MakeGenericMethod(confDataType);
                    bytes = (byte[])gm.Invoke(null, new object?[] { data, null });
                }

                var dir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                File.WriteAllBytes(outPath, bytes);
                Debug.Log($"[配置表] 数据已序列化到 bytes -> {outPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError("[配置表] 写 bytes 失败: " + ex);
            }
        }

        // 安全获取类型，避免某些程序集抛异常
        private static IEnumerable<Type> SafeGetTypes(Assembly asm)
        {
            try { return asm.GetTypes(); }
            catch { return Array.Empty<Type>(); }
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
        private static void ReadExcelFile(
            string filePath,
            Type elemType,
            FieldInfo[] subFields,
            object dataObj,
            string currentClassName,
            FieldInfo targetField)
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            do
            {
                string sheetName = reader.Name;
                if (ReadConfEditorUtil.ToCamelUpper(sheetName) != currentClassName)
                    continue;

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

                    var rowObj = Activator.CreateInstance(elemType)!;
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
                targetField.SetValue(dataObj, arr);

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
