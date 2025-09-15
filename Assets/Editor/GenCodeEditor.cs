using System.Collections.Generic; 
using System.Globalization;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelDataReader;
using UnityEditor; // 引入用于处理 Excel 文件的库

namespace ReadConf
{
    public static class GenCodeEditor
    {
        public static GenCodeContent DoGenConfCode()
        {
            string[] csvFiles = Directory.GetFiles(Application.dataPath + ResEditorConfig.CSV_Path, "*.csv", SearchOption.AllDirectories);
            string[] excelFiles = Directory.GetFiles(Application.dataPath + ResEditorConfig.CSV_Path, "*.xlsx", SearchOption.AllDirectories);
            string[] xlsFiles = Directory.GetFiles(Application.dataPath + ResEditorConfig.CSV_Path, "*.xls", SearchOption.AllDirectories);

            string tip = "/// <summary>\n/// /// 此代码为自动生成, 修改无意义, 重新生成会被覆盖\n/// </summary>\n\n";
            
            string codeStr = "using MemoryPack;\nusing System.Collections.Generic;\n\n";

            string fieldStr = "[MemoryPackable]\npublic partial class ConfData\n{\n";
            string defStr = "";

            GenCodeContent result = new()
            {
                classes = new List<ClassContent>()
            };
            
            //按所属文件夹分组
            var folderClasses = new Dictionary<string, List<(string className, string classFieldName)>>();

            #region 解析csv
            foreach (string file in csvFiles)
            {
                Debug.Log("[配置表][Editor] " + file);

                // 解析 CSV 文件
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,           
                    Delimiter = ",",                  
                    TrimOptions = TrimOptions.None,   
                    PrepareHeaderForMatch = args => args.Header.Trim(),
                    BadDataFound = null               
                };
                List<string> propertyNames;
                List<string> propertyTypes;
                using (var reader = new StreamReader(file))
                using (var csv = new CsvReader(reader, config))
                {
                    csv.Read();
                    csv.ReadHeader();
                    propertyNames = csv.HeaderRecord.ToList();

                    if (!csv.Read())
                    {
                        Debug.LogWarning($"[警告] 文件 {file} 格式不正确，缺少类型行。");
                        continue;
                    }

                    propertyTypes = new List<string>();
                    foreach (var header in propertyNames)
                    {
                        propertyTypes.Add(csv.GetField(header));
                    }
                }

                if (propertyNames.Count != propertyTypes.Count)
                {
                    Debug.LogWarning($"[警告] 文件 {file} 属性名和类型数量不一致。");
                    continue;
                }

                string className = Path.GetFileNameWithoutExtension(file);
                className = ReadConfEditorUtil.ToCamelUpper(className);
                string classDef = $"[MemoryPackable]\npublic partial class {className}\n{{\n";

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
                
                // 收集到 folderClasses
                string folderName = Path.GetFileName(Path.GetDirectoryName(file));
                if (!folderClasses.TryGetValue(folderName, out var list))
                {
                    list = new List<(string, string)>();
                    folderClasses[folderName] = list;
                }
                list.Add((className, classFieldName));
                
                result.classes.Add(new ClassContent
                {
                    className = className,
                    fileName = className,
                    FilePath = file.Replace("/", "\\"),
                    classDef = classDef,
                    props = props,
                });
            }
            #endregion

            #region 解析Excel格式
            foreach (string file in excelFiles.Concat(xlsFiles))
            {
                Debug.Log("[配置表][Editor] " + file);

                // 解析 Excel 文件的每个子表
                using (var stream = File.Open(file, FileMode.Open, FileAccess.Read))
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    do
                    {
                        string sheetName = reader.Name;
                        List<string> propertyNames = new List<string>();
                        List<string> propertyTypes = new List<string>();

                        bool isHeader = true;
                        while (reader.Read())
                        {
                            if (isHeader)
                            {
                                // 读取表头
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    propertyNames.Add(reader.GetString(i)?.Trim() ?? string.Empty);
                                }
                                isHeader = false;
                            }
                            else
                            {
                                // 读取类型行
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    propertyTypes.Add(reader.GetString(i)?.Trim() ?? "string");
                                }
                                break;
                            }
                        }

                        if (propertyNames.Count != propertyTypes.Count)
                        {
                            Debug.LogWarning($"[警告] 文件 {file} 的工作表 {sheetName} 属性名和类型数量不一致。");
                            continue;
                        }

                        string className = ReadConfEditorUtil.ToCamelUpper(sheetName);
                        string classDef = $"[MemoryPackable]\npublic partial class {className}\n{{\n";

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

                        // 收集到 folderClasses
                        string folderName = Path.GetFileName(Path.GetDirectoryName(file));
                        if (!folderClasses.TryGetValue(folderName, out var list))
                        {
                            list = new List<(string, string)>();
                            folderClasses[folderName] = list;
                        }
                        list.Add((className, classFieldName));

                        result.classes.Add(new ClassContent
                        {
                            className = className,
                            fileName = className,
                            FilePath = file.Replace("/", "\\"),
                            classDef = classDef,
                            props = props,
                        });
                    } while (reader.NextResult()); // 切换到下一个子表
                }
            }
            #endregion

            codeStr = tip + codeStr;
            codeStr += (fieldStr + "}\n\n" + defStr);

            Debug.Log(codeStr);

            File.WriteAllText(Application.dataPath + ResEditorConfig.ConfData_Path, codeStr, System.Text.Encoding.UTF8);
            
            //生成racastSet
            DoGenRacastSet(folderClasses);
            
            EditorUtility.DisplayDialog(
                "配置表生成",
                "所有配置表及 RacastSet 已生成完成！",
                "确定"
            );
                
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
                "float" => "float",
                "double" => "double",
                "bool" => "bool",
                "List<int>" => "List<int>",
                _ => "string",
            };
        }

        private static void DoGenRacastSet(Dictionary<string, List<(string className, string classFieldName)>> folderClasses)
        {
            foreach (var kv in folderClasses)
            {
                string folderName  = kv.Key;               // 目录名，如 "Enemy"
                var  classes       = kv.Value;             // List<(className, classFieldName)>
                string structName  = $"{folderName}RacastSet";

                var sb = new StringBuilder();
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("using System.Linq;");
                sb.AppendLine();
                sb.AppendLine($"public struct {structName} : IRacastSet");
                sb.AppendLine("{");
                // 5.1 属性：public Dictionary<int,T> TtCt { get; private set; }
                foreach (var (className, _) in classes)
                {
                    sb.AppendLine($"    public Dictionary<int, {className}> {className}Ct {{ get; private set; }}");
                }
                sb.AppendLine();
                // 5.2 构造函数：从 ConfData 一次性初始化所有字典
                sb.AppendLine($"    public {structName}(ConfData data)");
                sb.AppendLine("    {");
                foreach (var (className, classFieldName) in classes)
                {
                    sb.AppendLine($"        {className}Ct = data.{classFieldName}.ToDictionary(es => es.id);");
                }
                sb.AppendLine("    }");
                sb.AppendLine("}");

                // 5.3 写入到文件
                string outPath = Path.Combine(
                    Application.dataPath + ResEditorConfig.Racast_Path,
                    $"{structName}.cs"
                );
                File.WriteAllText(outPath, sb.ToString());
                Debug.Log($"[配置表][Editor] 生成 {structName}.cs 完成");
            }
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
        public string FilePath;
        public string classDef;
        public List<GenCodeProp> props;
    }

    public class GenCodeContent
    {
        public List<ClassContent> classes;
    }
}
