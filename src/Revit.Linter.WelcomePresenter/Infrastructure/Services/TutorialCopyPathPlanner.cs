namespace Revit.Linter.WelcomePresenter.Infrastructure.Services;

using System.IO;

/// <summary>Plans a fresh path below the application-owned temporary tutorial root.</summary>
internal static class TutorialCopyPathPlanner
{
    public static string GetVersionRoot(int revitVersion) => Path.Combine(
        Path.GetTempPath(), "Revit Linter", "Tutorial", revitVersion.ToString());

    public static string Create(string sourceFile, int revitVersion)
    {
        string sessionDirectory = Path.Combine(
            GetVersionRoot(revitVersion), Guid.NewGuid().ToString("N"));
        return Path.Combine(sessionDirectory, Path.GetFileName(sourceFile));
    }

    public static bool TryGetSessionDirectory(string path, int revitVersion, out string? sessionDirectory)
    {
        string versionRoot = Path.GetFullPath(GetVersionRoot(revitVersion));
        string? candidate = Path.GetDirectoryName(Path.GetFullPath(path));
        sessionDirectory = candidate;
        return candidate is not null
               && string.Equals(Path.GetDirectoryName(candidate), versionRoot, StringComparison.OrdinalIgnoreCase)
               && Guid.TryParseExact(Path.GetFileName(candidate), "N", out _);
    }
}
