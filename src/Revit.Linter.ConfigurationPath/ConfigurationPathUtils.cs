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

    public static void EnsureFileExists(string path)
    {
        string? directoryPath = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Diagnostic configuration file not found.");
        System.IO.Directory.CreateDirectory(directoryPath);

        if (!File.Exists(path))
            File.WriteAllText(path, string.Empty);
    }
}
