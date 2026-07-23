namespace Revit.Linter.Build;

internal static class BuildPaths
{
    public static string Root { get; } = FindRoot();
    public static string Solution => Path.Combine(Root, "Revit.Linter.slnx");
    public static string AddInProject => Path.Combine(Root, "src", "Revit.Linter", "Revit.Linter.csproj");
    public static string InstallerProject => Path.Combine(Root, "installer", "Revit.Linter.Installer", "Revit.Linter.Installer.csproj");

    public static string GetAddInOutput(string configuration)
    {
        string configurationDirectory = Path.Combine(
            Root, "src", "Revit.Linter", "bin", "x64", configuration);

        return Directory
            .GetDirectories(configurationDirectory)
            .Single(directory => File.Exists(Path.Combine(directory, "Revit.Linter.dll")));
    }

    public static string GetInstallerExecutable() =>
        Path.Combine(Root, "installer", "Revit.Linter.Installer", "bin", "Release", "net8.0-windows", "Revit.Linter.Installer.exe");

    private static string FindRoot()
    {
        DirectoryInfo? directory = new(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Revit.Linter.slnx")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not find the Revit.Linter repository root.");
    }
}
