using Revit.Linter.Localization;

namespace Revit.Linter.UserDiagnostics;

internal static class UserDiagnosticLocalizations
{
    private const string ResourceBaseName = "Revit.Linter.Localization.UserDiagnostics.UserDiagnosticLocalizations";

    public static string GetString(string key) => LocalizationResourceReader.GetString(ResourceBaseName, key);
}
