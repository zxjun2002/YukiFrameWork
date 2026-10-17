using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine.Events;

internal sealed class UGUIEventDescriptor
{
    public string name;
    public Type[] argumentTypes;

    public string Parameters => string.Join(", ", argumentTypes.Select((type, index) =>
        $"{UGUIScriptGeneratorEvents.TypeName(type)} value{index}"));
    public string Label => $"{name} ({string.Join(", ", argumentTypes.Select(type => type.Name))})";
}

internal static class UGUIScriptGeneratorEvents
{
    private static readonly Dictionary<Type, List<UGUIEventDescriptor>> Cache =
        new Dictionary<Type, List<UGUIEventDescriptor>>();
    private static readonly Dictionary<Type, string> TypeKeywords = new Dictionary<Type, string>
    {
        { typeof(void), "void" }, { typeof(bool), "bool" }, { typeof(byte), "byte" },
        { typeof(sbyte), "sbyte" }, { typeof(short), "short" }, { typeof(ushort), "ushort" },
        { typeof(int), "int" }, { typeof(uint), "uint" }, { typeof(long), "long" },
        { typeof(ulong), "ulong" }, { typeof(float), "float" }, { typeof(double), "double" },
        { typeof(decimal), "decimal" }, { typeof(char), "char" }, { typeof(string), "string" },
        { typeof(object), "object" }
    };

    public static List<UGUIEventDescriptor> GetEvents(Type componentType)
    {
        if (componentType == null) return new List<UGUIEventDescriptor>();
        if (Cache.TryGetValue(componentType, out var cached)) return cached;

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        var members = componentType.GetFields(flags)
            .Where(field => !field.IsDefined(typeof(ObsoleteAttribute), true))
            .Select(field => new { field.Name, Type = field.FieldType })
            .Concat(componentType.GetProperties(flags)
                .Where(property => property.GetIndexParameters().Length == 0 &&
                    property.GetGetMethod() != null && !property.IsDefined(typeof(ObsoleteAttribute), true))
                .Select(property => new { property.Name, Type = property.PropertyType }));

        var result = new List<UGUIEventDescriptor>();
        foreach (var member in members.GroupBy(member => member.Name).Select(group => group.First()))
        {
            if (!typeof(UnityEventBase).IsAssignableFrom(member.Type)) continue;
            // 使用 UnityAction 的签名，避免误选自定义事件额外重载的 Invoke。
            var addListener = member.Type.GetMethods(flags).FirstOrDefault(method =>
                method.Name == "AddListener" && method.GetParameters().Length == 1 &&
                typeof(Delegate).IsAssignableFrom(method.GetParameters()[0].ParameterType));
            var invoke = addListener?.GetParameters()[0].ParameterType.GetMethod("Invoke");
            if (invoke == null || invoke.ReturnType != typeof(void)) continue;
            var arguments = invoke.GetParameters();
            if (arguments.Any(argument => argument.ParameterType.IsByRef ||
                argument.ParameterType.IsPointer || argument.ParameterType.ContainsGenericParameters)) continue;
            result.Add(new UGUIEventDescriptor
            {
                name = member.Name,
                argumentTypes = arguments.Select(argument => argument.ParameterType).ToArray()
            });
        }

        result = result.OrderBy(descriptor => descriptor.name, StringComparer.Ordinal).ToList();
        Cache.Add(componentType, result);
        return result;
    }

    public static IEnumerable<string> TypeNamespaces(Type type)
    {
        if (TypeKeywords.ContainsKey(type)) yield break;
        if (type.IsArray)
        {
            foreach (string value in TypeNamespaces(type.GetElementType())) yield return value;
            yield break;
        }
        if (!string.IsNullOrEmpty(type.Namespace)) yield return type.Namespace;
        foreach (var argument in type.GetGenericArguments())
            foreach (string value in TypeNamespaces(argument)) yield return value;
    }

    public static bool CanUseShortTypeName(Type type, IEnumerable<string> namespaces, string codeNamespace, string className)
    {
        if (TypeKeywords.ContainsKey(type)) return true;
        if (type.IsArray) return CanUseShortTypeName(type.GetElementType(), namespaces, codeNamespace, className);
        if (type.GetGenericArguments().Any(argument => !CanUseShortTypeName(argument, namespaces, codeNamespace, className))) return false;

        var rootType = type;
        while (rootType.DeclaringType != null) rootType = rootType.DeclaringType;
        if (rootType.IsGenericType) rootType = rootType.GetGenericTypeDefinition();
        if (rootType.Name.Split('`')[0] == className) return false;
        var lookupNamespaces = new HashSet<string>(namespaces) { "" };
        for (string scope = codeNamespace; !string.IsNullOrEmpty(scope);)
        {
            lookupNamespaces.Add(scope);
            int separator = scope.LastIndexOf('.');
            scope = separator < 0 ? "" : scope.Substring(0, separator);
        }
        foreach (string scope in lookupNamespaces)
        {
            string fullName = string.IsNullOrEmpty(scope) ? rootType.Name : scope + "." + rootType.Name;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var candidate = assembly.GetType(fullName, false);
                if (candidate != null && candidate != rootType) return false;
            }
        }
        return true;
    }

    public static string TypeName(Type type, bool useShortName = false)
    {
        if (TypeKeywords.TryGetValue(type, out string keyword)) return keyword;
        if (type.IsArray) return TypeName(type.GetElementType(), useShortName) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        string fullName = type.IsGenericType ? type.GetGenericTypeDefinition().FullName : type.FullName;
        if (useShortName && !string.IsNullOrEmpty(type.Namespace)) fullName = fullName.Substring(type.Namespace.Length + 1);
        if (!type.IsGenericType) return (useShortName ? "" : "global::") + fullName.Replace('+', '.');

        // DeclaringType 的泛型实参也包含在 GetGenericArguments 中。
        var names = fullName.Split('+');
        var arguments = type.GetGenericArguments();
        int offset = 0;
        return (useShortName ? "" : "global::") + string.Join(".", names.Select(name =>
        {
            int tick = name.IndexOf('`');
            if (tick < 0) return name;
            int count = int.Parse(name.Substring(tick + 1));
            string formatted = name.Substring(0, tick) + "<" +
                string.Join(", ", arguments.Skip(offset).Take(count).Select(argument => TypeName(argument, useShortName))) + ">";
            offset += count;
            return formatted;
        }));
    }

    public static bool ContainsMethod(string source, string methodName)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(methodName)) return false;
        // 屏蔽注释和字符串；调用、AddListener 和 TODO 中出现方法名都不是方法声明。
        string code = UGUIScriptGeneratorSource.MaskNonCode(source);
        return Regex.IsMatch(code, @"\bvoid\s+@?" + Regex.Escape(methodName) + @"\s*\(");
    }
}
