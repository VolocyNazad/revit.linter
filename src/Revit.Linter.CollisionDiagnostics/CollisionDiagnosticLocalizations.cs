using Revit.Linter.Localization;

namespace Revit.Linter.CollisionDiagnostics;

internal static class CollisionDiagnosticLocalizations
{
    private const string ResourceBaseName = "Revit.Linter.Localization.CollisionDiagnostics.CollisionDiagnosticLocalizations";

    public static string GetString(string key) => LocalizationResourceReader.GetString(ResourceBaseName, key);
}
