using Revit.Linter.Localization;

namespace Revit.Linter.Diagnostic;

internal static class DiagnosticLocalizations
{
    private const string ResourceBaseName = "Revit.Linter.Localization.Diagnostic.DiagnosticLocalizations";

    public static string GetString(string key) => LocalizationResourceReader.GetString(ResourceBaseName, key);
}
