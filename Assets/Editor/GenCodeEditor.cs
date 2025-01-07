using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace ReadConf
{
    /// <summary>
    /// 此代码为自动生成,不要随便修改
    /// </summary>
    
    public static class GenCodeEditor
    {
        public static GenCodeContent DoGenConfCode()
        {
            string[] files = Directory.GetFiles(Application.dataPath + ResEditorConfig.CSV_Path, "*.csv", SearchOption.AllDirectories);

            string tip = "/// <summary>\n/// 此代码为自动生成,修改无意义重新生成会被后覆盖\n/// </summary>\n\n";
            
            string codeStr = "using MemoryPack;\n\n";

            string fieldStr = "[MemoryPackable]\npublic partial class ConfData{\n";
            string defStr = "";

            GenCodeContent result = new()
            {
                classes = new List<ClassContent>()
            };

            foreach (string file in files)
            {
                Debug.Log("[配置表][Editor] " + file);

                // 解析 CSV 文件
                // 配置 CSVHelper 选项
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,           // CSV 第一行是表头
                    Delimiter = ",",                  // 默认分隔符
                    TrimOptions = TrimOptions.None,   // 不自动修剪空格
                    PrepareHeaderForMatch = args => args.Header.Trim(), // 表头字段处理
                    BadDataFound = null               // 忽略坏数据
                };
                using (var reader = new StreamReader(file))
                using (var csv = new CsvReader(reader, config))
                {
                    // 读取表头
                    csv.Read();
                    csv.ReadHeader();
                    var propertyNames = csv.HeaderRecord.ToList();

                    // 读取类型行
                    if (!csv.Read())
                    {
                        Debug.LogWarning($"[警告] 文件 {file} 格式不正确，缺少类型行。");
                        continue;
                    }

                    // 获取类型数据
                    var propertyTypes = new List<string>();
                    foreach (var header in propertyNames)
                    {
                        propertyTypes.Add(csv.GetField(header));
                    }

                    if (propertyNames.Count != propertyTypes.Count)
                    {
                        Debug.LogWarning($"[警告] 文件 {file} 属性名和类型数量不一致。");
                        continue;
                    }

                    // 生成类
                    string className = Path.GetFileNameWithoutExtension(file);
                    string classDef = $"[MemoryPackable]\npublic partial class {className} {{\n";

                    List<GenCodeProp> props = new();

                    for (int i = 0; i < propertyNames.Count; i++)
                    {
                        string propName = ReadConfEditorUtil.ToCamelLower(propertyNames[i]);
                        string propType = MapCsvTypeToCSharpType(propertyTypes[i]);

                        classDef += $"    public {propType} {propName};\n";

                        props.Add(new GenCodeProp { name = propName, propType = propType });
                    }

                    classDef += "}\n\n";

                    string classFieldName = char.ToLower(className[0]) + className[1..];
                    fieldStr += $"    public {className}[] {classFieldName};\n";
                    defStr += classDef;

                    result.classes.Add(new ClassContent
                    {
                        className = className,
                        fileName = className,
                        CsvPath = file.Replace("/", "\\"),
                        classDef = classDef,
                        props = props,
                    });
                }

            }

            codeStr = tip + codeStr;
            codeStr += (fieldStr + "}\n\n" + defStr);

            Debug.Log(codeStr);

            File.WriteAllText(Application.dataPath + ResEditorConfig.ConfData_Path, codeStr, System.Text.Encoding.UTF8);

            return result;
        }

        private static string MapCsvTypeToCSharpType(string csvType)
        {
            return csvType switch
            {
                "int" => "int",
                "string" => "string",
                "long" => "long",
                "int[]" => "int[]",
                "string[]" => "string[]",
                _ => "string", // 默认类型
            };
        }
    }

    public class GenCodeProp
    {
        public string name;
        public string propType;
    }

    public class ClassContent
    {
        public string className;
        public string fileName;
        public string CsvPath;
        public string classDef;
        public List<GenCodeProp> props;
    }

    public class GenCodeContent
    {
        public List<ClassContent> classes;
    }

}