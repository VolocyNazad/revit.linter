using Autodesk.Revit.DB;

namespace Revit.Linter.ParameterElementDiagnostics.Infrastructure.Utils;

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
}
