using UnityEditor;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace ReadConf
{
    public class ReadConfEditor
    {

        [MenuItem("Tool/读取配置表")]
        public static void ReadConf()
        {
            GenCodeContent content = GenCodeEditor.DoGenConfCode();

            UnityEngine.Debug.Log("代码生成成功");

            ReadConfData(content);
        }

        private static void ReadConfData(GenCodeContent content)
        {

            ConfData data = new ConfData();

            System.Reflection.FieldInfo[] fields = typeof(ConfData).GetFields();

            StringBuilder strBld = new StringBuilder();

            foreach (System.Reflection.FieldInfo field in fields)
            {
                Type fieldType = field.FieldType.GetElementType();
                System.Reflection.FieldInfo[] subFields = fieldType.GetFields();

                string className = char.ToUpper(field.Name[0]) + field.Name[1..];
                //UnityEngine.Debug.Log("--- " + className);

                ClassContent cc =
                    (from cc_ in content.classes
                        where cc_.className == className
                        select cc_).First();
                string filePath = cc.CsvPath;
                  try
                  {
                      // 配置 CSVHelper 选项
                      var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                      {
                          HasHeaderRecord = true,           // CSV 第一行是表头
                          Delimiter = ",",                  // 默认分隔符
                          TrimOptions = TrimOptions.None,   // 不自动修剪空格
                          PrepareHeaderForMatch = args => args.Header.Trim(), // 表头字段处理
                          BadDataFound = null               // 忽略坏数据
                      };

                      // 使用 CSVReader 读取文件
                      using (var reader = new StreamReader(filePath))
                      using (var csv = new CsvReader(reader, config))
                      {
                          // 读取表头
                          csv.Read();
                          csv.ReadHeader();
                          var headerNames = csv.HeaderRecord;

                          // 将表头转换为驼峰格式
                          for (int i = 0; i < headerNames.Length; i++)
                          {
                              headerNames[i] = ReadConfEditorUtil.ToCamelLower(headerNames[i]);
                          }

                          // 动态列表存储结果
                          var confList = new List<object>();

                          while (csv.Read())
                          {
                              // 为每一行创建对象
                              var rowObj = Activator.CreateInstance(fieldType);

                              for (int j = 0; j < subFields.Length; j++)
                              {
                                  try
                                  {
                                      // 获取字段元数据
                                      var subFieldType = subFields[j].FieldType;
                                      var subFieldName = subFields[j].Name;

                                      // 找到对应列索引
                                      int fieldIndex = Array.IndexOf(headerNames, subFieldName);
                                      if (fieldIndex < 0) continue; // 表头未找到对应字段

                                      // 获取 CSV 中的值
                                      var value = csv.GetField(fieldIndex);

                                      // 转换类型并赋值
                                      object val = ConvertValue(value, subFieldType);
                                      subFields[j].SetValue(rowObj, val);
                                  }
                                  catch (Exception ex)
                                  {
                                      UnityEngine.Debug.LogError($"请检查配置表: {field} 的 [{confList.Count}, {j}] 错误是 {ex.Message}");
                                  }
                              }

                              // 添加对象到结果列表
                              confList.Add(rowObj);
                          }

                          // 转换结果列表为数组并赋值
                          var confArr = Array.CreateInstance(fieldType, confList.Count);
                          confList.CopyTo((object[])confArr);
                          field.SetValue(data, confArr);
                      }
                  }
                  catch (Exception e)
                  {
                      UnityEngine.Debug.LogError($"解析 CSV 文件 {filePath} 时出错: {e.Message}");
                  }

                ConfData serConf = new ConfData();
                serConf = data;

                FileBytesUtil.SaveConfDataToByteFile(serConf, ResEditorConfig.ConfsAsset_Path);
            }
        }

        // 辅助方法：将 CSV 的字符串值转换为目标类型
        private static object ConvertValue(string value, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(value)) return GetDefaultValue(targetType);

            if (targetType == typeof(int))
                return int.TryParse(value, out int intVal) ? intVal : 0;

            if (targetType == typeof(long))
                return long.TryParse(value, out long longVal) ? longVal : 0L;

            if (targetType == typeof(float))
                return float.TryParse(value, out float floatVal) ? floatVal : 0f;

            if (targetType == typeof(double))
                return double.TryParse(value, out double doubleVal) ? doubleVal : 0d;

            if (targetType == typeof(bool))
                return bool.TryParse(value, out bool boolVal) ? boolVal : false;

            return value; // 默认作为字符串返回
        }

        // 辅助方法：获取类型的默认值
        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
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
                result += char.ToUpper(nameStrs[i][0]);
                result += nameStrs[i][1..];
            }
            return result;
        }

        public static string ToCamelLower(string str)
        {
            string result = "";
            string[] nameStrs = str.Split("_");
            result += nameStrs[0];
            for (int i = 1; i < nameStrs.Length; i++)
            {
                result += char.ToUpper(nameStrs[i][0]);
                result += nameStrs[i][1..];
            }
            return result;
        }
    }
}