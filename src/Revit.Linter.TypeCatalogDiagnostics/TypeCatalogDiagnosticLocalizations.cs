using Revit.Linter.Localization;

namespace Revit.Linter.TypeCatalogDiagnostics;

internal static class TypeCatalogDiagnosticLocalizations
{
    private const string ResourceBaseName =
        "Revit.Linter.Localization.TypeCatalogDiagnostics.TypeCatalogDiagnosticLocalizations";

    public static string GetString(string key) => LocalizationResourceReader.GetString(ResourceBaseName, key);
}
