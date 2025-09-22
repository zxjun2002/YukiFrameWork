using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelDataReader;
using Newtonsoft.Json;
using System;

namespace ReadConf
{
    public static class GenCodeEditor
    {
        // ======= 外部入口：生成代码（按 side 过滤：C/S/ALL，缺省 C） =======
        public static GenCodeContent DoGenConfCode(string side = "C")
        {
            var content = BuildGenCodeContent(side);

            string tip = "/// <summary>\n/// 此代码为自动生成, 修改无意义, 重新生成会被覆盖\n/// </summary>\n\n";
            string codeStr = "using MemoryPack;\nusing System.Collections.Generic;\n\n";

            string fieldStr = "[MemoryPackable]\npublic partial class ConfData\n{\n";
            string defStr = "";

            // 按所属文件夹分组（用于 RacastSet）
            var folderClasses = new Dictionary<string, List<(string className, string classFieldName)>>();

            foreach (var c in content.classes)
            {
                string classFieldName = char.ToLower(c.className[0]) + c.className[1..];
                fieldStr += $"    public {c.className}[] {classFieldName};\n";
                defStr   += c.classDef;

                string folderName = Path.GetFileName(Path.GetDirectoryName(c.FilePath));
                if (!folderClasses.TryGetValue(folderName, out var list))
                {
                    list = new List<(string, string)>();
                    folderClasses[folderName] = list;
                }
                list.Add((c.className, classFieldName));
            }

            fieldStr += "}\n\n";
            codeStr = tip + codeStr + fieldStr + defStr;

            File.WriteAllText(Application.dataPath + ResEditorConfig.ConfData_Path, codeStr, Encoding.UTF8);
            Debug.Log("[配置表][Editor] ConfData.cs 生成完成");

            // 生成 RacastSet（根据 schema 的 pk_order 生成多层索引）
            DoGenRacastSet(folderClasses, content.classes);

            return content;
        }

        // ======= 外部入口：仅扫描（用于编译后重建映射再读数据） =======
        public static GenCodeContent ScanOnly(string side = "C") => BuildGenCodeContent(side);

        // ======= 读取文件 + schema，产出 GenCodeContent（核心） =======
        private static GenCodeContent BuildGenCodeContent(string side = "C")
        {
            side = NormalizeSide(side);

            string root = Application.dataPath + ResEditorConfig.Config_Path;
            string[] csvFiles   = Directory.GetFiles(root, "*.csv",  SearchOption.AllDirectories);
            string[] xlsxFiles  = Directory.GetFiles(root, "*.xlsx", SearchOption.AllDirectories);
            string[] xlsFiles   = Directory.GetFiles(root, "*.xls",  SearchOption.AllDirectories);

            GenCodeContent result = new() { classes = new List<ClassContent>() };

            // ---------- CSV：首行表头 + schema（表级侧别过滤；列按 schema 顺序/存在性） ----------
            foreach (string file in csvFiles)
            {
                var headerSet = ReadCsvHeaderSet(file);
                if (headerSet.Count == 0) { Debug.LogWarning($"[警告] CSV 无表头：{file}"); continue; }

                var sp = SchemaPathFor(file);
                if (!File.Exists(sp)) { Debug.LogWarning($"[警告] 缺少 schema：{sp}"); continue; }
                var ts = ReadJson<TableSchema>(sp) ?? new TableSchema();

                var tableSide = ResolveSide(ts.table_properties, "ALL");
                if (!MatchesSide(tableSide, side))
                {
                    Debug.Log($"[Filter] CSV {Path.GetFileName(file)} cs={tableSide} side={side} => SKIP");
                    continue;
                }

                var orderedCols = (ts.columns ?? new()).Select((c, i) => (c, i))
                    .OrderBy(x => x.c.col_sort ?? x.i)
                    .Select(x => x.c);

                var props = new List<GenCodeProp>();
                var keyPriorities = new List<int?>();

                var className = ReadConfEditorUtil.ToCamelUpper(Path.GetFileNameWithoutExtension(file));
                var classDef  = new StringBuilder().AppendLine("[MemoryPackable]").AppendLine($"public partial class {className}").AppendLine("{");

                foreach (var col in orderedCols)
                {
                    string nameLower = ReadConfEditorUtil.ToCamelLower(col.col_name ?? "");
                    if (!headerSet.Contains(nameLower))
                    {
                        Debug.Log($"[Skip] CSV {Path.GetFileName(file)} 列 '{col.col_name}' 不在表头中");
                        continue;
                    }
                    string csharpType = MapCsvTypeToCSharpType(col.value_type);
                    classDef.AppendLine($"    public {csharpType} {nameLower};");
                    props.Add(new GenCodeProp { name = nameLower, propType = csharpType });
                    keyPriorities.Add(col.pk_order.HasValue && col.pk_order.Value > 0 ? col.pk_order : null);
                }

                classDef.AppendLine("}").AppendLine();

                if (props.Count == 0) { Debug.LogWarning($"[警告] CSV {file} 过滤后无列"); continue; }

                result.classes.Add(new ClassContent
                {
                    className     = className,
                    fileName      = className,
                    FilePath      = file.Replace("/", "\\"),
                    classDef      = classDef.ToString(),
                    props         = props,
                    keyPriorities = keyPriorities
                });
            }

            // ---------- Excel：每个 sheet 一个类；只读首行表头；按 workbook schema ----------
            foreach (string file in xlsxFiles.Concat(xlsFiles))
            {
                var sp = SchemaPathFor(file);
                if (!File.Exists(sp)) { Debug.LogWarning($"[警告] 缺少 workbook schema：{sp}"); continue; }
                var wb = ReadJson<WorkbookSchema>(sp) ?? new WorkbookSchema();

                using var stream = File.Open(file, FileMode.Open, FileAccess.Read);
                using var reader = ExcelReaderFactory.CreateReader(stream);

                do
                {
                    string sheetName = reader.Name;
                    var sheetSch = wb.sheets?.FirstOrDefault(s =>
                        s.sheet_name.Equals(sheetName, StringComparison.OrdinalIgnoreCase));
                    if (sheetSch == null)
                    {
                        Debug.LogWarning($"[警告] {Path.GetFileName(file)} 中的工作表 '{sheetName}' 不在 schema 中，跳过");
                        continue;
                    }

                    // 读首行表头
                    var headerSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                            headerSet.Add(ReadConfEditorUtil.ToCamelLower(reader.GetValue(i)?.ToString()?.Trim() ?? string.Empty));
                    }
                    if (headerSet.Count == 0)
                    {
                        Debug.LogWarning($"[警告] {Path.GetFileName(file)}/{sheetName} 无表头，跳过");
                        continue;
                    }

                    // 表/Sheet 级侧别过滤（列不额外覆盖）
                    var sheetSide = ResolveSide(sheetSch.sheet_properties, "ALL");
                    if (!MatchesSide(sheetSide, side))
                    {
                        Debug.Log($"[Filter] XLSX {Path.GetFileName(file)}/{sheetName} cs={sheetSide} side={side} => SKIP");
                        continue;
                    }

                    var orderedCols = (sheetSch.columns ?? new()).Select((c, i) => (c, i))
                        .OrderBy(x => x.c.col_sort ?? x.i)
                        .Select(x => x.c);

                    var props = new List<GenCodeProp>();
                    var keyPriorities = new List<int?>();

                    string className = ReadConfEditorUtil.ToCamelUpper(sheetName);
                    var classDef = new StringBuilder().AppendLine("[MemoryPackable]").AppendLine($"public partial class {className}").AppendLine("{");

                    foreach (var col in orderedCols)
                    {
                        string nameLower = ReadConfEditorUtil.ToCamelLower(col.col_name ?? "");
                        if (!headerSet.Contains(nameLower))
                        {
                            Debug.Log($"[Skip] XLSX {Path.GetFileName(file)}/{sheetName} 列 '{col.col_name}' 不在表头中");
                            continue;
                        }
                        string csharpType = MapCsvTypeToCSharpType(col.value_type);
                        classDef.AppendLine($"    public {csharpType} {nameLower};");
                        props.Add(new GenCodeProp { name = nameLower, propType = csharpType });
                        keyPriorities.Add(col.pk_order.HasValue && col.pk_order.Value > 0 ? col.pk_order : null);
                    }

                    classDef.AppendLine("}").AppendLine();

                    if (props.Count == 0)
                    {
                        Debug.LogWarning($"[警告] Excel {file}/{sheetName} 过滤后无列");
                        continue;
                    }

                    result.classes.Add(new ClassContent
                    {
                        className     = className,
                        fileName      = className,
                        FilePath      = file.Replace("/", "\\"),
                        classDef      = classDef.ToString(),
                        props         = props,
                        keyPriorities = keyPriorities
                    });

                } while (reader.NextResult());
            }

            return result;
        }

        // ====== CSV/Excel 表头读取（只读第一行） ======
        private static HashSet<string> ReadCsvHeaderSet(string file)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                TrimOptions = TrimOptions.None,
                PrepareHeaderForMatch = args => args.Header.Trim(),
                BadDataFound = null
            };
            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader, config);
            if (!csv.Read() || !csv.ReadHeader()) return set;
            foreach (var h in csv.HeaderRecord)
                set.Add(ReadConfEditorUtil.ToCamelLower(h));
            return set;
        }

        // ====== RacastSet 生成（保持你原来的风格） ======
        private static void DoGenRacastSet(
            Dictionary<string, List<(string className, string classFieldName)>> folderClasses,
            List<ClassContent> allClasses)
        {
            string outDir = Application.dataPath + ResEditorConfig.Racast_Path;
            Directory.CreateDirectory(outDir);

            var meta = allClasses.ToDictionary(c => c.className, c => c);

            foreach (var kv in folderClasses)
            {
                string folderName = string.IsNullOrEmpty(kv.Key) ? "Root" : kv.Key;
                var classes = kv.Value;
                string typeName = $"{folderName}RacastSet";

                string setDir = Path.Combine(outDir, typeName);
                Directory.CreateDirectory(setDir);

                var sb = new StringBuilder();
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("using System.Linq;");
                sb.AppendLine();
                sb.AppendLine($"public sealed partial class {typeName} : IRacastSet");
                sb.AppendLine("{");
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
                    if (chain.Count == 0) continue;
                    sb.AppendLine(EmitIndexCtorCt(className, fieldName, m.props, chain));
                }
                sb.AppendLine("        OnAfterInit();");
                sb.AppendLine("    }");
                sb.AppendLine();
                sb.AppendLine("    // 在 *.Logic.cs 中实现；未实现则无开销");
                sb.AppendLine("    partial void OnAfterInit();");
                sb.AppendLine("}");

                string autoPath = Path.Combine(setDir, $"{typeName}.cs");
                File.WriteAllText(autoPath, sb.ToString(), Encoding.UTF8);

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
                }
            }

            // 本地小工具
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
            static string EmitIndexFieldDeclCt(string className, List<GenCodeProp> props, List<string> keys)
            {
                string KeyType(int i) => props.First(p => p.name == keys[i]).propType;
                string TailType(int i) => (i == keys.Count - 1)
                    ? className
                    : $"Dictionary<{KeyType(i + 1)}, {TailType(i + 1)}>";
                return $"    public Dictionary<{KeyType(0)}, {TailType(0)}> {className}Ct {{ get; private set; }}";
            }
            static string EmitIndexCtorCt(string className, string fieldName, List<GenCodeProp> props, List<string> keys)
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
                sb.Append($".ToDictionary(g{last} => g{last}.Key, g{last} => g{last}.ToDictionary(e => e.{keys.Last()}, e => e));");
                return sb.ToString();
            }
        }

        // ====== 类型映射（完全由 schema value_type 决定） ======
        private static string MapCsvTypeToCSharpType(string csvType)
        {
            var t = (csvType ?? "").Trim().ToLowerInvariant();
            return t switch
            {
                "int" or "int32" or "i32"                 => "int",
                "long" or "int64" or "i64"                => "long",
                "float" or "single" or "float32" or "f32" => "float",
                "double" or "float64" or "f64"            => "double",
                "bool" or "boolean"                       => "bool",
                "int[]" or "int32[]" or "i32[]"           => "int[]",
                "list<int>" or "list<int32>" or "int list"=> "List<int>",
                _                                          => "string",
            };
        }

        // ====== schema & 侧别工具 ======
        private static string SchemaPathFor(string tablePath) => tablePath + ".schema.json";
        private static T? ReadJson<T>(string path)
        {
            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                // Newtonsoft.Json 大小写默认不敏感；这里也可加一些容错设置
                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    Culture = System.Globalization.CultureInfo.InvariantCulture
                };
                return JsonConvert.DeserializeObject<T>(json, settings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[schema] 读取/解析失败：{path}\n{ex}");
                return default;
            }
        }
        private static string NormalizeSide(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "ALL";
            s = s.Trim().ToUpperInvariant();
            return s is "C" or "S" or "ALL" or "NONE" ? s : "ALL";
        }
        private static string ResolveSide(List<SchemaProperty>? props, string fallback = "ALL")
        {
            var v = props?.FirstOrDefault(p => string.Equals(p.property_name, "cs_type", StringComparison.OrdinalIgnoreCase))?.property_value;
            return NormalizeSide(string.IsNullOrWhiteSpace(v) ? fallback : v!);
        }
        private static bool MatchesSide(string cs, string target)
        {
            cs = NormalizeSide(cs); target = NormalizeSide(target);
            if (cs == "NONE") return false;
            if (target == "ALL") return cs != "NONE";
            return cs == "ALL" || cs == target;
        }

        // ====== 模型 ======
        class SchemaProperty { public string property_name { get; set; } = ""; public string property_value { get; set; } = ""; }
        class ColumnSchema
        {
            public string col_name { get; set; } = "";
            public string value_type { get; set; } = "string";
            public int?   pk_order { get; set; } = null;
            public int?   col_sort { get; set; } = null;
            public List<SchemaProperty> col_properties { get; set; } = new();
        }
        class TableSchema
        {
            public string table_name { get; set; } = "";
            public List<ColumnSchema> columns { get; set; } = new();
            public List<SchemaProperty> table_properties { get; set; } = new();
        }
        class SheetSchema
        {
            public string sheet_name { get; set; } = "";
            public List<ColumnSchema> columns { get; set; } = new();
            public List<SchemaProperty> sheet_properties { get; set; } = new();
        }
        class WorkbookSchema
        {
            public string workbook_name { get; set; } = "";
            public List<SheetSchema> sheets { get; set; } = new();
        }

        // ====== 导出给外部用的数据模型（保持你原样） ======
        public class GenCodeProp { public string name; public string propType; }
        public class ClassContent
        {
            public string className;
            public string fileName;
            public string FilePath;
            public string classDef;
            public List<GenCodeProp> props;
            public List<int?> keyPriorities; // 来自 schema 的 pk_order
        }
        public class GenCodeContent { public List<ClassContent> classes; }
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
