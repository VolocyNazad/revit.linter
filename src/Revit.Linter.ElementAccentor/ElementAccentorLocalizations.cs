using Revit.Linter.Localization;

namespace Revit.Linter.ElementAccentor;

internal static class ElementAccentorLocalizations
{
    private const string ResourceBaseName = "Revit.Linter.Localization.ElementAccentor.ElementAccentorLocalizations";

    public static string GetString(string key) => LocalizationResourceReader.GetString(ResourceBaseName, key);
}
