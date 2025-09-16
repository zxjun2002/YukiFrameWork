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
    public class ReadConfEditor
    {
        [MenuItem("Tool/配置表/配置表本地生成")]
        public static void ReadConf()
        {
            GenCodeContent content = GenCodeEditor.DoGenConfCode();
            Debug.Log("代码生成成功");
            ReadConfData(content);
        }
        
        /// <summary>
        /// 解析配置文件数据到 ConfData 对象。
        /// 支持 .csv、.xlsx 和 .xls 文件。
        /// </summary>
        private static void ReadConfData(GenCodeContent content)
        {
            ConfData data = new ConfData();

            FieldInfo[] fields = typeof(ConfData).GetFields();

            foreach (FieldInfo field in fields)
            {
                Type fieldType = field.FieldType.GetElementType();
                FieldInfo[] subFields = fieldType.GetFields();

                string className = char.ToUpper(field.Name[0]) + field.Name[1..];

                // 从生成内容中找到对应的 ClassContent
                ClassContent cc = content.classes.FirstOrDefault(cc_ => cc_.className == className);
                if (cc == null)
                {
                    Debug.LogWarning($"未找到对应的 ClassContent: {className}");
                    continue;
                }

                string filePath = cc.FilePath;
                try
                {
                    List<string> headerNames = new List<string>();
                    List<object> confList = new List<object>();

                    // 根据文件类型选择解析方法
                    if (filePath.EndsWith(".csv"))
                    {
                        ReadCsvFile(filePath, fieldType, subFields, headerNames, confList);
                        // 将解析结果赋值给对应的字段
                        var confArr = Array.CreateInstance(fieldType, confList.Count);
                        confList.CopyTo((object[])confArr);
                        field.SetValue(data, confArr);
                    }
                    else if (filePath.EndsWith(".xlsx") || filePath.EndsWith(".xls"))
                    {
                        ReadExcelFile(filePath, content, fieldType, subFields, data, className);
                    }
                    else
                    {
                        Debug.LogWarning($"不支持的文件格式: {filePath}");
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"解析文件 {filePath} 时出错: {e.Message}");
                }
            }

            // 保存解析结果到二进制文件
            FileBytesUtil.SaveConfDataToByteFile(data, ResEditorConfig.ConfsAsset_Path);
        }

        /// <summary>
        /// 使用 CsvHelper 解析 CSV 文件。
        /// </summary>
        private static void ReadCsvFile(string filePath, Type fieldType, FieldInfo[] subFields,
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

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, config))
            {
                // 第1行：表头
                csv.Read();
                csv.ReadHeader();
                headerNames.AddRange(csv.HeaderRecord.Select(ReadConfEditorUtil.ToCamelLower));

                // 第2行：优先级（跳过）
                if (!csv.Read())
                {
                    Debug.LogWarning($"文件 {filePath} 缺少优先级行，跳过解析");
                    return;
                }
                // 第3行：类型（跳过）
                if (!csv.Read())
                {
                    Debug.LogWarning($"文件 {filePath} 缺少类型行，跳过解析");
                    return;
                }

                // 从第4行开始：逐行读取数据
                while (csv.Read())
                {
                    var rowObj = Activator.CreateInstance(fieldType);

                    for (int i = 0; i < subFields.Length; i++)
                    {
                        try
                        {
                            string subFieldName = subFields[i].Name;
                            int fieldIndex = headerNames.IndexOf(subFieldName);
                            if (fieldIndex < 0) continue;

                            string value = csv.GetField(fieldIndex);
                            object val = ConvertValue(value, subFields[i].FieldType);
                            subFields[i].SetValue(rowObj, val);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"CSV 文件解析错误: {ex.Message}");
                        }
                    }

                    confList.Add(rowObj);
                }
            }
        }
        
        /// <summary>
        /// 使用 ExcelDataReader 解析 Excel 文件的所有工作表（支持 .xlsx 和 .xls）。
        /// </summary>
        private static void ReadExcelFile(string filePath, GenCodeContent content, Type fieldType, FieldInfo[] subFields, ConfData data, string currentClassName)
        {
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                do
                {
                    string sheetName = reader.Name;

                    // 找到与当前工作表匹配的类
                    if (ReadConfEditorUtil.ToCamelUpper(sheetName) != currentClassName)
                    {
                        continue;
                    }

                    FieldInfo field = typeof(ConfData).GetField(currentClassName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (field == null)
                    {
                        Debug.LogWarning($"ConfData 中未找到对应字段: {currentClassName}");
                        continue;
                    }

                    List<string> headerNames = new List<string>();
                    List<object> confList = new List<object>();

                    bool isHeader = true;
                    bool prioritySkipped = false;
                    bool typeSkipped = false;

                    while (reader.Read())
                    {
                        if (isHeader)
                        {
                            // 第1行：表头
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                headerNames.Add(ReadConfEditorUtil.ToCamelLower(reader.GetString(i)?.Trim() ?? string.Empty));
                            }
                            isHeader = false;
                            continue;
                        }

                        if (!prioritySkipped) { prioritySkipped = true; continue; } // 第2行：优先级（跳过）
                        if (!typeSkipped)     { typeSkipped = true;     continue; } // 第3行：类型（跳过）

                        // —— 从这里开始是真实数据行 —— //
                        var rowObj = Activator.CreateInstance(fieldType);

                        for (int i = 0; i < subFields.Length; i++)
                        {
                            try
                            {
                                string subFieldName = subFields[i].Name;
                                int fieldIndex = headerNames.IndexOf(subFieldName);
                                if (fieldIndex < 0) continue;

                                string value = reader.IsDBNull(fieldIndex) ? null : reader.GetValue(fieldIndex)?.ToString();
                                object val = ConvertValue(value, subFields[i].FieldType);
                                subFields[i].SetValue(rowObj, val);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Excel 文件解析错误: {ex.Message}");
                            }
                        }

                        confList.Add(rowObj);
                    }

                    // 将解析结果赋值到对应字段
                    var confArr = Array.CreateInstance(fieldType, confList.Count);
                    confList.CopyTo((object[])confArr);
                    field.SetValue(data, confArr);
                } while (reader.NextResult()); // 切换到下一个工作表
            }
        }

        /// <summary>
        /// 将字符串值转换为目标类型。
        /// </summary>
        private static object ConvertValue(string value, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(value)) return GetDefaultValue(targetType);

            try
            {
                if (targetType == typeof(int))    return int.TryParse(value, out int intVal) ? intVal : 0;
                if (targetType == typeof(long))   return long.TryParse(value, out long longVal) ? longVal : 0L;
                if (targetType == typeof(float))  return float.TryParse(value, out float floatVal) ? floatVal : 0f;
                if (targetType == typeof(double)) return double.TryParse(value, out double doubleVal) ? doubleVal : 0d;
                if (targetType == typeof(bool))   return bool.TryParse(value, out bool boolVal) ? boolVal : false;
                if (targetType == typeof(int[]))  return ParseIntArray(value);
                if (targetType == typeof(List<int>)) return ParseIntList(value);
            }
            catch (Exception ex)
            {
                Debug.LogError($"ConvertValue 错误: {ex.Message}");
            }

            return value;
        }

        /// <summary>
        /// 获取类型的默认值。
        /// </summary>
        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        /// <summary>
        /// 解析以逗号分隔的整数数组。
        /// </summary>
        private static int[] ParseIntArray(string value)
        {
            value = value.Trim();
            if (value.StartsWith("[") && value.EndsWith("]"))
            {
                value = value.Substring(1, value.Length - 2);
            }
            return value.Split(',').Select(s => int.TryParse(s.Trim(), out int val) ? val : 0).ToArray();
        }

        private static List<int> ParseIntList(string value)
        {
            value = value.Trim();
            if (value.StartsWith("[") && value.EndsWith("]"))
            {
                value = value.Substring(1, value.Length - 2);
            }
            return value.Split(',').Select(s => int.TryParse(s.Trim(), out int val) ? val : 0).ToList();
        }
    }

    public static class ReadConfEditorUtil
    {
        public static string ToCamelUpper(string str)
        {
            string result = "";
            string[] nameStrs = str.Split("_");
            for (int i = 0; i < nameStrs.Length; i++)
            {
                if (string.IsNullOrEmpty(nameStrs[i])) continue;
                result += char.ToUpper(nameStrs[i][0]);
                if (nameStrs[i].Length > 1) result += nameStrs[i][1..];
            }
            return result;
        }

        public static string ToCamelLower(string str)
        {
            string result = "";
            string[] nameStrs = str.Split("_");
            if (nameStrs.Length == 0) return str;
            result += nameStrs[0];
            for (int i = 1; i < nameStrs.Length; i++)
            {
                if (string.IsNullOrEmpty(nameStrs[i])) continue;
                result += char.ToUpper(nameStrs[i][0]);
                if (nameStrs[i].Length > 1) result += nameStrs[i][1..];
            }
            return result;
        }
    }
}
