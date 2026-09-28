using Revit.Linter.Localization;
using System.Globalization;

namespace Revit.Linter.ParameterElementDiagnostics;

internal static class ParameterElementDiagnosticLocalizations
{
    private const string ResourceBaseName = "Revit.Linter.Localization.ParameterElementDiagnostics.ParameterElementDiagnosticLocalizations";

    public static string GetString(string key, params object[] arguments)
    {
        string value = LocalizationResourceReader.GetString(ResourceBaseName, key);
        return arguments.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, arguments);
    }
}
