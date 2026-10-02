namespace Revit.Linter.Testing;

/// <summary>Locates the repository checkout a test assembly was built from.</summary>
public static class RepositoryRoot
{
    private const string SolutionFileName = "Revit.Linter.slnx";

    /// <summary>
    /// Finds the repository root by walking up from the test output directory to the folder that
    /// contains the root solution.
    /// </summary>
    /// <returns>The absolute path of the repository root.</returns>
    /// <exception cref="DirectoryNotFoundException">No parent directory contains the root solution.</exception>
    public static string Find()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
