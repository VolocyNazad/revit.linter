using Revit.Linter.Localization;

namespace Revit.Linter.ElementIgnoring;

internal static class ElementIgnoringLocalizations
{
    private const string ResourceBaseName = "Revit.Linter.Localization.ElementIgnoring.ElementIgnoringLocalizations";

    public static string GetString(string key) => LocalizationResourceReader.GetString(ResourceBaseName, key);
}
