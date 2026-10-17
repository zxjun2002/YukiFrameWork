using System;
using System.Linq;
using System.Text.RegularExpressions;

// 只定位类和成员，不重新格式化用户代码。忽略注释和字符串中的大括号。
internal static class UGUIScriptGeneratorSource
{
    internal static Tuple<int, int> ClassBody(string source, string className)
    {
        string code = MaskNonCode(source);
        var declarations = Regex.Matches(code, @"\bclass\s+@?" + Regex.Escape(className) + @"\b");
        if (declarations.Count != 1)
            throw new InvalidOperationException($"无法唯一定位主类 {className}，请检查类声明。业务脚本未被覆盖。");
        int start = code.IndexOf('{', declarations[0].Index + declarations[0].Length);
        if (start < 0) throw new InvalidOperationException($"类 {className} 缺少开始大括号。");
        int depth = 1;
        for (int i = start + 1; i < code.Length; i++)
        {
            if (code[i] == '{') depth++;
            if (code[i] == '}' && --depth == 0) return Tuple.Create(start, i);
        }
        throw new InvalidOperationException($"类 {className} 的大括号未闭合，业务脚本未被覆盖。");
    }

    internal static string AppendMembers(string source, string className, string members)
    {
        if (string.IsNullOrWhiteSpace(members)) return source;
        var body = ClassBody(source, className);
        int lineStart = source.LastIndexOf('\n', body.Item2 - 1) + 1;
        int insertAt = string.IsNullOrWhiteSpace(source.Substring(lineStart, body.Item2 - lineStart)) ? lineStart : body.Item2;
        string prefix = insertAt > 0 && source[insertAt - 1] == '\n' ? "" : "\n";
        return source.Insert(insertAt, prefix + "\n" + members.Trim('\r', '\n') + "\n");
    }

    internal static string MergeLegacyMembers(string panel, string legacy, string className)
    {
        var body = ClassBody(legacy, className);
        string code = MaskNonCode(legacy);
        string outside = code.Substring(0, body.Item1) + code.Substring(body.Item2 + 1);
        if (Regex.Matches(outside, @"\b(class|struct|interface|enum|delegate)\b").Count != 1 ||
            outside.Contains('[') || Regex.IsMatch(outside, @"\bextern\s+alias\b"))
            throw new InvalidOperationException("旧 Bind.cs 包含额外类型、类型属性或 extern alias，请先移入主 .cs。旧脚本未被覆盖。");

        string prefix = code.Substring(0, body.Item1);
        foreach (Match directive in Regex.Matches(prefix, @"(?m)^[ \t]*using[ \t]+[^;\r\n]+;"))
        {
            string statement = legacy.Substring(directive.Index, directive.Length).Trim();
            if (!MaskNonCode(panel).Contains(statement)) panel = statement + "\n" + panel;
        }
        return AppendMembers(panel, className, legacy.Substring(body.Item1 + 1, body.Item2 - body.Item1 - 1));
    }

    internal static string MaskNonCode(string source)
    {
        char[] code = source.ToCharArray();
        for (int i = 0; i < source.Length;)
        {
            int end = SkipLiteralOrComment(source, i);
            if (end == i) { i++; continue; }
            for (int j = i; j < end; j++)
                if (code[j] != '\r' && code[j] != '\n') code[j] = ' ';
            i = end;
        }
        return new string(code);
    }

    private static int SkipLiteralOrComment(string source, int index)
    {
        if (source[index] == '#')
        {
            int lineStart = source.LastIndexOf('\n', index) + 1;
            if (source.Substring(lineStart, index - lineStart).All(char.IsWhiteSpace))
            {
                int end = source.IndexOf('\n', index + 1);
                return end < 0 ? source.Length : end;
            }
        }
        if (source[index] == '/' && index + 1 < source.Length)
        {
            if (source[index + 1] == '/')
            {
                int end = source.IndexOf('\n', index + 2);
                return end < 0 ? source.Length : end;
            }
            if (source[index + 1] == '*')
            {
                int end = source.IndexOf("*/", index + 2, StringComparison.Ordinal);
                if (end < 0) throw new InvalidOperationException("业务代码存在未闭合的注释。");
                return end + 2;
            }
        }

        int quote = index;
        while (quote < source.Length && quote - index < 2 && (source[quote] == '$' || source[quote] == '@')) quote++;
        if (quote >= source.Length || (source[quote] != '"' && (quote != index || source[quote] != '\''))) return index;
        bool character = source[quote] == '\'';
        bool verbatim = source.Substring(index, quote - index).Contains('@');
        bool interpolated = source.Substring(index, quote - index).Contains('$');
        for (int i = quote + 1; i < source.Length; i++)
        {
            if (!verbatim && source[i] == '\\') { i++; continue; }
            if (source[i] == (character ? '\'' : '"'))
            {
                if (verbatim && i + 1 < source.Length && source[i + 1] == '"') { i++; continue; }
                return i + 1;
            }
            if (interpolated && source[i] == '{')
            {
                if (i + 1 < source.Length && source[i + 1] == '{') { i++; continue; }
                i = SkipInterpolation(source, i + 1);
            }
        }
        throw new InvalidOperationException("业务代码存在未闭合的字符串或字符。");
    }

    private static int SkipInterpolation(string source, int index)
    {
        int depth = 1;
        for (int i = index; i < source.Length; i++)
        {
            int end = SkipLiteralOrComment(source, i);
            if (end > i) { i = end - 1; continue; }
            if (source[i] == '{') depth++;
            if (source[i] == '}' && --depth == 0) return i;
        }
        throw new InvalidOperationException("业务代码存在未闭合的字符串插值。");
    }
}
