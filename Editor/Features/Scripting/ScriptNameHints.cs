using System.Reflection;
using ECS;
using SceneComponents;
using Scripting;

namespace Editor.Features.Scripting;

internal static class ScriptNameHints
{
    private const int MinPrefix = 2;
    private const int MaxResults = 12;

    private static readonly string[] Catalog = BuildCatalog();

    public static string[] Match(string prefix, IEnumerable<string>? extraNames = null)
    {
        if (prefix.Length < MinPrefix)
            return [];

        IEnumerable<string> names = Catalog;
        if (extraNames is not null)
            names = names.Concat(extraNames);

        return
        [
            .. names
                .Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .Take(MaxResults)
        ];
    }

    public static string IdentifierPrefix(string line, int column, int tabSize = 4)
    {
        if (column <= 0 || line.Length == 0)
            return "";

        var index = 0;
        var col = 0;
        while (index < line.Length && col < column)
        {
            col = line[index] == '\t' ? col / tabSize * tabSize + tabSize : col + 1;
            index++;
        }

        var start = index;
        while (start > 0 && IsIdentChar(line[start - 1]))
            start--;

        return line[start..index];
    }

    private static bool IsIdentChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static string[] BuildCatalog()
    {
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract", "as", "async", "await", "base", "bool", "break", "byte", "case", "catch",
            "char", "checked", "class", "const", "continue", "decimal", "default", "delegate",
            "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
            "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
            "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator",
            "out", "override", "params", "private", "protected", "public", "readonly", "ref",
            "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
            "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong",
            "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
            "var", "nameof", "record", "when", "where", "yield", "Vector2", "Vector3", "Vector4",
            "TimeSpan",
        };

        AddPublicTypeNames(names, typeof(ScriptableEntity).Assembly);
        AddPublicTypeNames(names, typeof(Entity).Assembly);
        AddPublicTypeNames(names, typeof(NativeScriptComponent).Assembly);

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public
            | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        foreach (var member in typeof(ScriptableEntity).GetMembers(flags))
        {
            if (member is MethodInfo { IsSpecialName: false } or PropertyInfo)
                names.Add(member.Name);
        }

        return [.. names];
    }

    private static void AddPublicTypeNames(HashSet<string> names, Assembly assembly)
    {
        foreach (var type in assembly.GetExportedTypes())
            if (!type.Name.Contains('`', StringComparison.Ordinal))
                names.Add(type.Name);
    }
}
