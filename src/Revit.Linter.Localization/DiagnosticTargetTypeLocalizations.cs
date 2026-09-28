namespace Revit.Linter.Localization;

public static class DiagnosticTargetTypeLocalizations
{
    private const string ResourceBaseName = "Revit.Linter.Localization.DiagnosticTargetTypeLocalizations";

    public static string GetString(string targetType)
        => LocalizationResourceReader.GetString(ResourceBaseName, targetType);
}
