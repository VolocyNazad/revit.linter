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
}
