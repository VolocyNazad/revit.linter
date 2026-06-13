namespace Revit.Linter.ConfigurationPath;

public static class ConfigurationPathUtils
{
    private static int _revitVersion =
#if IS2021
2021;
#elif IS2023
    2023;
#elif IS2025
    2025;
#else
    throw new InvalidOperationException("Unsupported Revit version");
#endif

    public static readonly string Directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Revit Linter",
        _revitVersion.ToString()
    );
}
