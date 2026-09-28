using Revit.Linter.Localization;

namespace Revit.Linter.DocumentDiagnostics;

internal static class DocumentDiagnosticLocalizations
{
    private const string ResourceBaseName = "Revit.Linter.Localization.DocumentDiagnostics.DocumentDiagnosticLocalizations";

    public static string GetString(string key) => LocalizationResourceReader.GetString(ResourceBaseName, key);
}
