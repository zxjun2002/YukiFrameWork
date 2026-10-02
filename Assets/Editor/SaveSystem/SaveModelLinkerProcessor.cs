using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SQLite;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor.UnityLinker;

namespace Yuki.SaveSystem.Editor
{
    /// <summary>构建时自动保留 [Table] 模型及其成员，避免 IL2CPP 裁剪反射使用的属性。</summary>
    public sealed class SaveModelLinkerProcessor : IUnityLinkerProcessor
    {
        public int callbackOrder => 0;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            var playerAssemblies = new HashSet<string>(
                CompilationPipeline.GetAssemblies(AssembliesType.Player).Select(assembly => assembly.name));
            var models = new HashSet<Type>();
            foreach (var type in TypeCache.GetTypesWithAttribute<TableAttribute>())
            {
                // 也保留模型继承的属性；排除编辑器测试程序集。
                for (Type model = type; model != null && model != typeof(object); model = model.BaseType)
                    if (playerAssemblies.Contains(model.Assembly.GetName().Name)) models.Add(model);
            }
            var linker = new XElement("linker",
                models.GroupBy(type => type.Assembly.GetName().Name).OrderBy(group => group.Key).Select(group =>
                    new XElement("assembly", new XAttribute("fullname", group.Key),
                        group.OrderBy(type => type.FullName).Select(type =>
                            new XElement("type", new XAttribute("fullname", type.FullName.Replace('+', '/')),
                                new XAttribute("preserve", "all"))))));
            string path = Path.GetFullPath("Temp/YukiSaveSystem/link.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            new XDocument(linker).Save(path);
            return path;
        }
    }
}
