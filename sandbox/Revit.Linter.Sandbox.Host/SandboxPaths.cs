using System.Text.RegularExpressions;

namespace Revit.Linter.Sandbox.Host;

internal static class SandboxPaths
{
    private const string DefaultConfiguration = "Debug_2025.0.0";

    public static string ParseConfiguration(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] is "--configuration" or "-c")
                return args[i + 1];
        }

        return DefaultConfiguration;
    }

    public static int ParseMajorVersion(string configuration)
    {
        Match match = Regex.Match(configuration, @"\d{4}");
        if (!match.Success)
            throw new ArgumentException($"Configuration must contain a Revit version, got: {configuration}");

        return int.Parse(match.Value);
    }

    public static string FindRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Revit.Linter.Sandbox.slnx")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not find the sandbox root.");
    }

    public static string FindSandboxAssembly(string root, string configuration)
    {
        string configurationDirectory = Path.Combine(
            root, "Revit.Linter.Sandbox", "bin", "x64", configuration);

        if (!Directory.Exists(configurationDirectory))
            throw new DirectoryNotFoundException($"Sandbox build output not found: {configurationDirectory}");

        return Directory
            .GetDirectories(configurationDirectory)
            .Select(directory => Path.Combine(directory, "Revit.Linter.Sandbox.dll"))
            .FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException($"Revit.Linter.Sandbox.dll not found under {configurationDirectory}");
    }

    public static string FindManifestPath(int majorVersion) =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Autodesk",
            "Revit",
            "Addins",
            majorVersion.ToString(),
            "Revit.Linter.Sandbox.addin");

    public static string FindRevit(int majorVersion)
    {
        string[] roots =
        [
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        ];

        foreach (string root in roots)
        {
            if (string.IsNullOrEmpty(root))
                continue;

            string candidate = Path.Combine(root, "Autodesk", $"Revit {majorVersion}", "Revit.exe");
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Revit {majorVersion} not found.");
    }
}