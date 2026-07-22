namespace Revit.Linter.Languages.Utils;

internal static class ReflectionUtils
{
    public static object? GetPropertyValue(object? target, string propertyName)
    {
        if (target is null) return null;
        var prop = target.GetType().GetProperty(propertyName);
        if (prop == null) return null;
        return prop.GetValue(target);
    }
    public static object? InvokeMethod(object? target, string methodName)
    {
        if (target is null) return null;

        var method = target.GetType()
            .GetMethods()
            .FirstOrDefault(method =>
                method.Name == methodName &&
                method.GetParameters().Length == 0 &&
                !method.IsGenericMethodDefinition &&
                method.ReturnType != typeof(void));

        return method?.Invoke(target, null);
    }
}
