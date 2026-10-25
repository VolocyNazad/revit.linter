using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.Core.Abstractions;

namespace Revit.Linter.ConfigurationPath.Services;

/// <summary>Computes the diagnostic configuration paths for one Revit version and documents folder.</summary>
/// <remarks>
/// The production constructor selects the version from the build configuration and uses the current user's
/// Documents folder. Tests and host adapters can pass both values explicitly.
/// </remarks>
public sealed class ConfigurationPaths : IConfigurationPaths
{
    /// <summary>Creates the paths for the running build and the current user.</summary>
    public ConfigurationPaths()
        : this(TargetRevitVersion, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
    {
    }

    /// <summary>Creates the paths for an explicit Revit version and documents folder.</summary>
    /// <param name="revitVersion">The Revit release year, for example <c>2025</c>.</param>
    /// <param name="documentsDirectory">The user's Documents folder.</param>
    public ConfigurationPaths(int revitVersion, string documentsDirectory)
    {
        if (string.IsNullOrWhiteSpace(documentsDirectory))
            throw new ArgumentException("The documents directory is required.", nameof(documentsDirectory));

        RevitVersion = revitVersion;
        VersionsDirectory = Path.Combine(documentsDirectory, ProductIdentity.DocumentsFolderName);
        Directory = Path.Combine(VersionsDirectory, revitVersion.ToString());
        ConfigPath = Path.Combine(Directory, ConfigurationPathUtils.DiagnosticsConfigFileName);
        ExampleConfigPath = Path.Combine(
            Directory, ConfigurationPathUtils.ExamplesFolderName, ConfigurationPathUtils.DiagnosticsConfigFileName);
        DebugConfigPath = Path.Combine(
            Directory, ConfigurationPathUtils.DebugFolderName, ConfigurationPathUtils.DiagnosticsConfigFileName);
        PracticalTourConfigPath = Path.Combine(
            Directory, ConfigurationPathUtils.TourFolderName, ConfigurationPathUtils.PracticalTourFileName);
    }

    /// <inheritdoc />
    public int RevitVersion { get; }

    /// <inheritdoc />
    public string VersionsDirectory { get; }

    /// <inheritdoc />
    public string Directory { get; }

    /// <inheritdoc />
    public string ConfigPath { get; }

    /// <inheritdoc />
    public string ExampleConfigPath { get; }

    /// <inheritdoc />
    public string DebugConfigPath { get; }

    /// <inheritdoc />
    public string PracticalTourConfigPath { get; }

    private static int TargetRevitVersion =>
#if IS2021
        2021;
#elif IS2022
        2022;
#elif IS2023
        2023;
#elif IS2024
        2024;
#elif IS2025
        2025;
#elif IS2026
        2026;
#elif IS2027
        2027;
#else
        throw new InvalidOperationException("Unsupported Revit version");
#endif
}
