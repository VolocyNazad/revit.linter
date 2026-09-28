using Revit.Linter.Localization;

namespace Revit.Linter.ElementDiagnostics;

internal static class ElementDiagnosticLocalizations
{
    private const string ResourceBaseName = "Revit.Linter.Localization.ElementDiagnostics.ElementDiagnosticLocalizations";

    public static string GetString(string key) => LocalizationResourceReader.GetString(ResourceBaseName, key);
}
