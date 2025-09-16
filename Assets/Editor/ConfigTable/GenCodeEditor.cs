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
using System;

namespace ReadConf
{
    public static class GenCodeEditor
    {
        public static GenCodeContent DoGenConfCode()
        {
            string[] csvFiles = Directory.GetFiles(Application.dataPath + ResEditorConfig.Config_Path, "*.csv",
                SearchOption.AllDirectories);
            string[] excelFiles = Directory.GetFiles(Application.dataPath + ResEditorConfig.Config_Path, "*.xlsx",
                SearchOption.AllDirectories);
            string[] xlsFiles = Directory.GetFiles(Application.dataPath + ResEditorConfig.Config_Path, "*.xls",
                SearchOption.AllDirectories);

            string tip = "/// <summary>\n/// 此代码为自动生成, 修改无意义, 重新生成会被覆盖\n/// </summary>\n\n";
            string codeStr = "using MemoryPack;\nusing System.Collections.Generic;\n\n";

            string fieldStr = "[MemoryPackable]\npublic partial class ConfData\n{\n";
            string defStr = "";

            GenCodeContent result = new()
            {
                classes = new List<ClassContent>()
            };

            // 按所属文件夹分组
            var folderClasses = new Dictionary<string, List<(string className, string classFieldName)>>();

            #region 解析 CSV

            foreach (string file in csvFiles)
            {
                Debug.Log("[配置表][Editor] " + file);

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Delimiter = ",",
                    TrimOptions = TrimOptions.None,
                    PrepareHeaderForMatch = args => args.Header.Trim(),
                    BadDataFound = null
                };

                List<string> propertyNames;
                List<string> propertyTypes; // 第3行：类型
                List<int?> keyPriorities; // 第2行：优先级

                using (var reader = new StreamReader(file))
                using (var csv = new CsvReader(reader, config))
                {
                    // 第1行：列名
                    csv.Read();
                    csv.ReadHeader();
                    propertyNames = csv.HeaderRecord.ToList();

                    // 第2行：优先级
                    if (!csv.Read())
                    {
                        Debug.LogWarning($"[警告] 文件 {file} 缺少优先级行。");
                        continue;
                    }

                    keyPriorities = ParsePriorityRow(propertyNames.Count, i => csv.GetField(i));

                    // 第3行：类型
                    if (!csv.Read())
                    {
                        Debug.LogWarning($"[警告] 文件 {file} 缺少类型行。");
                        continue;
                    }

                    propertyTypes = new List<string>(propertyNames.Count);
                    for (int i = 0; i < propertyNames.Count; i++)
                        propertyTypes.Add(csv.GetField(i)?.Trim() ?? "string");
                }

                if (propertyNames.Count != propertyTypes.Count)
                {
                    Debug.LogWarning($"[警告] 文件 {file} 属性名和类型数量不一致。");
                    continue;
                }

                string className = ReadConfEditorUtil.ToCamelUpper(Path.GetFileNameWithoutExtension(file));
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
                    keyPriorities = keyPriorities, // ★ 第二行优先级
                });
            }

            #endregion

            #region 解析 Excel（含 .xlsx / .xls）

            foreach (string file in excelFiles.Concat(xlsFiles))
            {
                Debug.Log("[配置表][Editor] " + file);

                using (var stream = File.Open(file, FileMode.Open, FileAccess.Read))
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    do
                    {
                        string sheetName = reader.Name;

                        List<string> propertyNames = new();
                        List<string> propertyTypes = new(); // 第3行：类型
                        List<int?> keyPriorities = new(); // 第2行：优先级

                        bool gotHeader = false;
                        bool gotPriority = false;
                        bool gotType = false;

                        while (reader.Read())
                        {
                            if (!gotHeader)
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                    propertyNames.Add(reader.GetString(i)?.Trim() ?? string.Empty);
                                gotHeader = true;
                                continue;
                            }

                            if (!gotPriority)
                            {
                                keyPriorities = ParsePriorityRow(
                                    propertyNames.Count,
                                    i => reader.IsDBNull(i) ? "" : reader.GetValue(i)?.ToString()
                                );
                                gotPriority = true;
                                continue;
                            }

                            if (!gotType)
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                    propertyTypes.Add(reader.GetString(i)?.Trim() ?? "string");
                                gotType = true;
                                break; // 代码生成阶段只需前三行
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
                            keyPriorities = keyPriorities, // ★
                        });
                    } while (reader.NextResult()); // 下一子表
                }
            }

            #endregion

            // 写 ConfData.cs
            codeStr = tip + codeStr + (fieldStr + "}\n\n" + defStr);
            File.WriteAllText(Application.dataPath + ResEditorConfig.ConfData_Path, codeStr, Encoding.UTF8);
            Debug.Log("[配置表][Editor] ConfData.cs 生成完成");

            // 生成 RacastSet（含优先级多层字典）
            DoGenRacastSet(folderClasses, result.classes);

            EditorUtility.DisplayDialog(
                "配置表生成",
                "所有配置表及 RacastSet 已生成完成！",
                "确定"
            );

            return result;
        }

        // 解析“优先级行”：整数 → 优先级；空/非法 → null
        private static List<int?> ParsePriorityRow(int count, Func<int, string> getter)
        {
            var pri = new List<int?>(count);
            for (int i = 0; i < count; i++)
            {
                var cell = getter(i);
                if (string.IsNullOrWhiteSpace(cell))
                {
                    pri.Add(null);
                    continue;
                }

                if (int.TryParse(cell.Trim(), out int p) && p > 0) pri.Add(p);
                else pri.Add(null);
            }

            return pri;
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

        // 生成 RacastSet：类 + 双文件（.cs 覆盖 / .Logic.cs 首创） + 按第二行优先级产出多层字典
        private static void DoGenRacastSet(
            Dictionary<string, List<(string className, string classFieldName)>> folderClasses,
            List<ClassContent> allClasses)
        {
            string outDir = Application.dataPath + ResEditorConfig.Racast_Path;
            Directory.CreateDirectory(outDir);

            // className -> meta
            var meta = allClasses.ToDictionary(c => c.className, c => c);

            foreach (var kv in folderClasses)
            {
                string folderName = kv.Key; // 如 "Buff" / "Item" / "Mission"
                var classes = kv.Value;
                string typeName = $"{folderName}RacastSet";

                // 每个 RacastSet 独立子目录
                string setDir = Path.Combine(outDir, typeName);
                Directory.CreateDirectory(setDir);

                var sb = new StringBuilder();
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("using System.Linq;");
                sb.AppendLine();
                sb.AppendLine($"public sealed partial class {typeName} : IRacastSet");
                sb.AppendLine("{");

                // 字段：仅按优先级链生成，字段名固定 {ClassName}Ct
                foreach (var (className, _) in classes)
                {
                    var m = meta[className];
                    var chain = ExtractKeyChain(m);
                    if (chain.Count == 0)
                    {
                        sb.AppendLine($"    // {className}: 未配置优先级，未生成索引字段");
                        continue;
                    }

                    sb.AppendLine(EmitIndexFieldDeclCt(className, m.props, chain));
                }

                sb.AppendLine();
                sb.AppendLine($"    public {typeName}(ConfData data)");
                sb.AppendLine("    {");
                foreach (var (className, fieldName) in classes)
                {
                    var m = meta[className];
                    var chain = ExtractKeyChain(m);
                    if (chain.Count == 0) continue; // 无优先级：不生成

                    // 为 {ClassName}Ct 赋值（嵌套字典）
                    sb.AppendLine(EmitIndexCtorCt(className, fieldName, m.props, chain));
                }

                sb.AppendLine("        OnAfterInit();");
                sb.AppendLine("    }");
                sb.AppendLine();
                sb.AppendLine("    // 在 *.Logic.cs 中实现；未实现则无开销");
                sb.AppendLine("    partial void OnAfterInit();");
                sb.AppendLine("}");

                // 写入自动生成文件（覆盖）
                string autoPath = Path.Combine(setDir, $"{typeName}.cs");
                File.WriteAllText(autoPath, sb.ToString(), Encoding.UTF8);
                Debug.Log($"[配置表][Editor] 生成 {typeName}/{typeName}.cs 完成（已覆盖）");

                // 首次生成逻辑文件（不覆盖）
                string logicPath = Path.Combine(setDir, $"{typeName}.Logic.cs");
                if (!File.Exists(logicPath))
                {
                    var sbLogic = new StringBuilder();
                    sbLogic.AppendLine("// 自定义扩展：此文件仅首次生成，之后不会被覆盖");
                    sbLogic.AppendLine("using System.Collections.Generic;");
                    sbLogic.AppendLine("using System.Linq;");
                    sbLogic.AppendLine();
                    sbLogic.AppendLine($"public sealed partial class {typeName}");
                    sbLogic.AppendLine("{");
                    sbLogic.AppendLine("    partial void OnAfterInit()");
                    sbLogic.AppendLine("    {");
                    sbLogic.AppendLine("        // TODO: 在这里构建你的业务索引/缓存");
                    sbLogic.AppendLine("    }");
                    sbLogic.AppendLine("}");
                    File.WriteAllText(logicPath, sbLogic.ToString(), Encoding.UTF8);
                    Debug.Log($"[配置表][Editor] 创建 {typeName}/{typeName}.Logic.cs（首次生成）");
                }
                else
                {
                    Debug.Log($"[配置表][Editor] 保留 {typeName}/{typeName}.Logic.cs（已存在，不覆盖）");
                }
            }

            // ====== 本地帮助函数（保持与之前一致的优先级提取）======
            static List<string> ExtractKeyChain(ClassContent c)
            {
                var result = new List<string>();
                if (c.keyPriorities == null || c.keyPriorities.Count == 0) return result;

                var pairs = c.props.Zip(c.keyPriorities, (p, pr) => (p.name, pr))
                    .Where(t => t.pr.HasValue && t.pr.Value > 0)
                    .OrderBy(t => t.pr!.Value)
                    .ToList();

                foreach (var (name, _) in pairs) result.Add(name);
                return result;
            }

            // 声明：public Dictionary<K1, Dictionary<K2, ... , T>> {ClassName}Ct { get; private set; }
            static string EmitIndexFieldDeclCt(string className, List<GenCodeProp> props, List<string> keys)
            {
                string KeyType(int i) => props.First(p => p.name == keys[i]).propType;

                string TailType(int i) => (i == keys.Count - 1)
                    ? className
                    : $"Dictionary<{KeyType(i + 1)}, {TailType(i + 1)}>";

                return $"    public Dictionary<{KeyType(0)}, {TailType(0)}> {className}Ct {{ get; private set; }}";
            }

            // 赋值：{ClassName}Ct = data.field ...（根据 keys 构造嵌套 GroupBy / ToDictionary）
            static string EmitIndexCtorCt(string className, string fieldName, List<GenCodeProp> props,
                List<string> keys)
            {
                if (keys.Count == 1)
                    return $"        {className}Ct = data.{fieldName}.ToDictionary(e => e.{keys[0]}, e => e);";

                var sb = new StringBuilder();
                sb.Append($"        {className}Ct = data.{fieldName}.GroupBy(e => e.{keys[0]})");
                for (int i = 0; i < keys.Count - 2; i++)
                {
                    sb.Append($".ToDictionary(g{i} => g{i}.Key, g{i} => g{i}.GroupBy(e => e.{keys[i + 1]}))");
                    sb.AppendLine();
                    sb.Append(new string(' ', 8));
                }

                int last = keys.Count - 2;
                sb.Append(
                    $".ToDictionary(g{last} => g{last}.Key, g{last} => g{last}.ToDictionary(e => e.{keys.Last()}, e => e));");
                return sb.ToString();
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
        public List<int?> keyPriorities; // ★ 第二行优先级（null=不参与；1/2/3…）
    }

    public class GenCodeContent
    {
        public List<ClassContent> classes;
    }
}