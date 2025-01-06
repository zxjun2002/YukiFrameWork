using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace ReadConf
{
    /// <summary>
    /// 此代码为自动生成,不要随便修改
    /// </summary>
    public static class GenCodeEditor
    {

        //[MenuItem("Tools/TestGenCode")]
        public static GenCodeContent DoGenConfCode()
        {
            string[] files = Directory.GetFiles(Application.dataPath + ResEditorConfig.CSV_Path, "*.json", SearchOption.AllDirectories);

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
                Debug.Log("[配置表][Editor]"+file);
                if (file.Contains("game_params") || file.Contains("table_desc"))
                {
                    Debug.LogWarning("[配置表][Editor]:跳过game_params.csv");
                    continue;
                }

                string str = System.IO.File.ReadAllText(file, System.Text.Encoding.UTF8);
                JObject jo = JObject.Parse(str);
                string tavle_property = jo["table_properties"] == null || jo["table_properties"].Count() == 0
                    ? string.Empty
                    : jo["table_properties"].Where(d => d["property_name"].Value<string>() == "cs_type").FirstOrDefault()[
                        "property_value"].Value<string>();
                
                if (tavle_property != "ALL" && tavle_property != "C")
                {
                    continue;
                }
                
                GameLogger.LogYellow("[生成配置表]"+file);
                //-------------class name
                ClassContent content = WriteClass(jo, file);
                string classFieldName = char.ToLower(content.className[0]) + content.className[1..];
                fieldStr += "    public " + content.className + "[] " + classFieldName + ";\n";
                defStr += content.classDef;

                result.classes.Add(content);
            }

            codeStr = tip + codeStr;
            
            codeStr += (fieldStr + "}\n\n" + defStr);


            Debug.Log(codeStr);

            File.WriteAllText(Application.dataPath + ResEditorConfig.ConfData_Path, codeStr, System.Text.Encoding.UTF8);

            return result;
        }

        private static ClassContent WriteClass(JObject jo, string jsonPath)
        {
            string name = jo["table_name"].Value<string>();
            string nameResult = ReadConfEditorUtil.ToCamelUpper(name);
            List<GenCodeProp> props = new List<GenCodeProp>();
            foreach (var col in jo["columns"])
            {
                string col_type = col["col_properties"] == null || col["col_properties"].Count() == 0
                    ? string.Empty
                    : col["col_properties"].Where(d => d["property_name"].Value<string>() == "cs_type").FirstOrDefault()[
                        "property_value"].Value<string>();
                if (col_type == "ALL" || col_type == "C")
                {
                    GenCodeProp prop = new GenCodeProp()
                    {
                        name = ReadConfEditorUtil.ToCamelLower(col["col_name"].Value<string>()),
                        propType = col["value_type"].Value<string>()
                    };
                    props.Add(prop);
                }
            }

            foreach (GenCodeProp prop in props)
            {
                if (prop.propType == "text")
                {
                    prop.propType = "string";
                }
                else if (prop.propType == "snowflake")
                {
                    prop.propType = "int";
                }
                else if (prop.propType == "tinyint")
                {
                    prop.propType = "int";
                }
                else if (prop.propType == "bigint")
                {
                    prop.propType = "int";
                }
            }

            string result = "[MemoryPackable]\npublic partial class " + nameResult + " {\n";

            foreach (JObject col in jo["columns"])
            {
                string col_type = col["col_properties"] == null || col["col_properties"].Count() == 0
                    ? string.Empty
                    : col["col_properties"].Where(d => d["property_name"].Value<string>() == "cs_type").FirstOrDefault()[
                        "property_value"].Value<string>();
                if (col_type != "ALL" && col_type != "C")
                {
                    continue;
                }
                string nameStr = ReadConfEditorUtil.ToCamelLower(col["col_name"].Value<string>());
                string propType = col["value_type"].Value<string>();
                if (propType == "text")
                {
                    propType = "string";
                }
                else if (propType == "snowflake")
                {
                    propType = "long";
                }
                else if (propType == "tinyint")
                {
                    propType = "int";
                }
                else if (propType == "bigint")
                {
                    propType = "int";
                }

                result += ("    public " + propType + " " + nameStr + ";\n");
            }


            result += "}\n\n";
            return new ClassContent
            {
                className = nameResult,
                fileName = name,
                CsvPath = jsonPath.Replace("/", "\\").Replace(".json", ".csv"),
                classDef = result,
                props = props,
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