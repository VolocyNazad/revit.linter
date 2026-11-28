using Revit.Linter.Localization;

namespace Revit.Linter.SizeTableDiagnostics;

internal static class SizeTableDiagnosticLocalizations
{
    private const string ResourceBaseName =
        "Revit.Linter.Localization.SizeTableDiagnostics.SizeTableDiagnosticLocalizations";

    public static string GetString(string key) => LocalizationResourceReader.GetString(ResourceBaseName, key);
}
