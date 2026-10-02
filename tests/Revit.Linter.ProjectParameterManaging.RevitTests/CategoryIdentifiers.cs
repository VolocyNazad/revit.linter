using Autodesk.Revit.DB;

namespace Revit.Linter.ProjectParameterManaging.RevitTests;

internal static class CategoryIdentifiers
{
    /// <summary>
    /// Returns the numeric form a configuration file uses for a built-in category. The underlying enum type
    /// is 32-bit through Revit 2023 and 64-bit from Revit 2024.
    /// </summary>
    public static string ToNumeric(BuiltInCategory category) =>
#if BEFORE2024
        ((int)category).ToString();
#else
        ((long)category).ToString();
#endif
}
