using Revit.Linter.Localization;
using System.Globalization;

namespace Revit.Linter.ElementVisualization;

internal static class ElementVisualizationLocalizations
{
    private const string ResourceBaseName = "Revit.Linter.Localization.ElementVisualization.ElementVisualizationLocalizations";

    public static string GetString(string key, params object[] arguments)
    {
        string value = LocalizationResourceReader.GetString(ResourceBaseName, key);
        return arguments.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, arguments);
    }
}
