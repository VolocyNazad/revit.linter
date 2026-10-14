using System.Collections.Concurrent;
using System.Reflection;

namespace Revit.Linter.Languages.Utils;

internal static class ReflectionUtils
{
    // Reflection metadata of a type never changes within a session, while these helpers run
    // once per element per formula evaluation, so resolved members are cached per (type, name).
    // Misses are cached too: an unknown property keeps returning null without a new lookup.
    private static readonly ConcurrentDictionary<(Type TargetType, string PropertyName), PropertyInfo?> PropertyCache = new();
    private static readonly ConcurrentDictionary<(Type TargetType, string MethodName), MethodInfo?> MethodCache = new();

    public static object? GetPropertyValue(object? target, string propertyName)
    {
        if (target is null) return null;
        PropertyInfo? property = PropertyCache.GetOrAdd(
            (target.GetType(), propertyName),
            static key => key.TargetType.GetProperty(key.PropertyName));
        return property?.GetValue(target);
    }
    public static object? InvokeMethod(object? target, string methodName)
    {
        if (target is null) return null;

        MethodInfo? method = MethodCache.GetOrAdd(
            (target.GetType(), methodName),
            static key => key.TargetType
                .GetMethods()
                .FirstOrDefault(candidate =>
                    candidate.Name == key.MethodName &&
                    candidate.GetParameters().Length == 0 &&
                    !candidate.IsGenericMethodDefinition &&
                    candidate.ReturnType != typeof(void)));

        return method?.Invoke(target, null);
    }
}
