using Autodesk.Revit.DB;

namespace Revit.Linter.ParameterElementDiagnostics.Infrastructure.Utils;

/// <summary>
/// Isolates the Revit-version differences in how configuration files identify categories and parameter groups.
/// </summary>
internal static class ParameterIdentifierParser
{
#if BEFORE2024
    public static BuiltInParameterGroup ParseGroup(string value) =>
        int.TryParse(value, out int id)
            ? (BuiltInParameterGroup)id
            : (BuiltInParameterGroup)Enum.Parse(typeof(BuiltInParameterGroup), value);
#endif

    public static BuiltInCategory ParseCategory(string value) =>
#if BEFORE2024
        int.TryParse(value, out int id)
            ? (BuiltInCategory)id
            : (BuiltInCategory)Enum.Parse(typeof(BuiltInCategory), value);
#else
        long.TryParse(value, out long id)
            ? (BuiltInCategory)id
            : Enum.Parse<BuiltInCategory>(value);
#endif

#if !BEFORE2024
    public static ForgeTypeId ParseGroupTypeId(string value) => new(value);
#endif

    /// <summary>
    /// Returns whether <paramref name="value"/> is a built-in category name or numeric identifier that
    /// <see cref="ParseCategory"/> accepts.
    /// </summary>
    public static bool IsKnownCategory(string value)
    {
#if BEFORE2024
        if (int.TryParse(value, out int id))
            return Enum.IsDefined(typeof(BuiltInCategory), (BuiltInCategory)id);
#else
        if (long.TryParse(value, out long id))
            return Enum.IsDefined(typeof(BuiltInCategory), (BuiltInCategory)id);
#endif
        // Enum.TryParse also accepts comma-separated lists, which are not a single category.
        return Enum.TryParse(value, out BuiltInCategory category)
            && Enum.IsDefined(typeof(BuiltInCategory), category)
            && value.IndexOf(',') < 0;
    }

    /// <summary>Returns whether <paramref name="value"/> identifies a parameter group in the running Revit.</summary>
    /// <remarks>
    /// Through Revit 2023 a group is a <c>PG_*</c> name or its numeric identifier. Revit 2024 and later use a
    /// full type identifier such as <c>autodesk.parameter.group:data-1.0.0</c>; an empty value denotes the
    /// group without an identifier, and a legacy <c>PG_*</c> name or number is rejected because it would
    /// never match a parameter.
    /// </remarks>
    public static bool IsKnownGroup(string value)
    {
#if BEFORE2024
        if (int.TryParse(value, out int id))
            return Enum.IsDefined(typeof(BuiltInParameterGroup), (BuiltInParameterGroup)id);

        return Enum.TryParse(value, out BuiltInParameterGroup group)
            && Enum.IsDefined(typeof(BuiltInParameterGroup), group)
            && value.IndexOf(',') < 0;
#else
        return value.Length == 0 || value.IndexOf(':') > 0;
#endif
    }
}
