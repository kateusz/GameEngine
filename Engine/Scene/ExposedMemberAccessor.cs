using System.Collections.Concurrent;
using System.Numerics;
using System.Reflection;

namespace Engine.Scene;

public static class ExposedMemberAccessor
{
    private static readonly ConcurrentDictionary<Type, FieldInfo[]> FieldCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    public static IEnumerable<(string Name, Type Type, object Value)> GetExposedMembers(object instance)
    {
        var type = instance.GetType();
        var fields = GetCachedFields(type);
        foreach (var field in fields)
        {
            yield return (field.Name, field.FieldType, field.GetValue(instance)!);
        }

        var properties = GetCachedProperties(type);
        foreach (var prop in properties)
        {
            yield return (prop.Name, prop.PropertyType, prop.GetValue(instance)!);
        }
    }

    public static object GetMemberValue(object instance, string name)
    {
        var type = instance.GetType();

        var field = Array.Find(GetCachedFields(type), f => f.Name == name);
        if (field != null)
            return field.GetValue(instance)!;

        var prop = Array.Find(GetCachedProperties(type), p => p.Name == name);
        if (prop != null && prop.CanRead)
            return prop.GetValue(instance)!;

        throw new ArgumentException($"Field or property '{name}' not found or not supported.");
    }

    public static void SetMemberValue(object instance, string name, object value)
    {
        var type = instance.GetType();

        var field = Array.Find(GetCachedFields(type), f => f.Name == name);
        if (field != null)
        {
            field.SetValue(instance, ConvertToSupportedType(value, field.FieldType));
            return;
        }

        var prop = Array.Find(GetCachedProperties(type), p => p.Name == name);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(instance, ConvertToSupportedType(value, prop.PropertyType));
            return;
        }

        throw new ArgumentException($"Field or property '{name}' not found or not supported.");
    }

    private static FieldInfo[] GetCachedFields(Type type)
    {
        return FieldCache.GetOrAdd(type, t =>
            t.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(f => IsSupportedType(f.FieldType))
                .ToArray());
    }

    private static PropertyInfo[] GetCachedProperties(Type type)
    {
        return PropertyCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite && IsSupportedType(p.PropertyType))
                .ToArray());
    }

    private static bool IsSupportedType(Type type)
    {
        return type == typeof(int) || type == typeof(float) || type == typeof(double) ||
               type == typeof(bool) || type == typeof(string) ||
               type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4);
    }

    private static object ConvertToSupportedType(object? value, Type targetType)
    {
        if (value == null)
            return targetType.IsValueType ? Activator.CreateInstance(targetType)! : null!;

        if (targetType.IsInstanceOfType(value))
            return value;

        if (targetType == typeof(Vector2) && value is System.Text.Json.Nodes.JsonArray { Count: 2 } arr2)
            return new Vector2((float)arr2[0]!, (float)arr2[1]!);

        if (targetType == typeof(Vector3) && value is System.Text.Json.Nodes.JsonArray { Count: 3 } arr3)
            return new Vector3((float)arr3[0]!, (float)arr3[1]!, (float)arr3[2]!);

        if (targetType == typeof(Vector4) && value is System.Text.Json.Nodes.JsonArray { Count: 4 } arr4)
            return new Vector4((float)arr4[0]!, (float)arr4[1]!, (float)arr4[2]!, (float)arr4[3]!);

        return Convert.ChangeType(value, targetType)!;
    }
}
