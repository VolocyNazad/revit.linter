using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.ConfigurationPath.Services;
using Revit.Linter.DiagnosticConfiguration;
using Revit.Linter.DiagnosticConfiguration.Models;
using Revit.Linter.DiagnosticConfiguration.Parsing;

namespace Revit.Linter.ConfigurationPath;

/// <summary>
/// Locates, creates, and reads the version-specific Revit Linter configuration files.
/// </summary>
public static class ConfigurationPathUtils
{
    /// <summary>Gets the single diagnostic configuration file name used by the current format.</summary>
    public const string DiagnosticsConfigFileName = DiagnosticConfigurationFormat.FileName;

    /// <summary>Gets the legacy element diagnostic configuration file name, moved into the legacy folder once.</summary>
    public const string LegacyConfigFileName = "config.yaml";

    /// <summary>Gets the legacy collision configuration file name, moved into the legacy folder once.</summary>
    public const string LegacyCollisionConfigFileName = "collision.config.yaml";

    /// <summary>Gets the legacy project-parameter configuration file name, moved into the legacy folder once.</summary>
    public const string LegacyParameterElementConfigFileName = "parameter-element.config.yaml";

    /// <summary>Gets the managed practical-tour configuration file name.</summary>
    public const string PracticalTourFileName = "practical-tour.config.yaml";

    /// <summary>Gets the subfolder that receives examples without overwriting user files.</summary>
    public const string ExamplesFolderName = "examples";

    /// <summary>Gets the subfolder that holds the managed practical-tour configuration.</summary>
    public const string TourFolderName = "tour";

    /// <summary>Gets the subfolder that holds debug configurations.</summary>
    /// <remarks>
    /// Debug rules are user-owned like the top-level files and outrank installed examples;
    /// unlike the examples folder, its files are never installed by the product.
    /// </remarks>
    public const string DebugFolderName = "debug";

    private static readonly IConfigurationPaths _defaultPaths = new ConfigurationPaths();

    /// <summary>
    /// Gets the Revit release year this build targets, for example <c>2025</c>.
    /// </summary>
    /// <remarks>Application code resolves <see cref="IConfigurationPaths"/> from dependency injection.</remarks>
    public static int RevitVersion => _defaultPaths.RevitVersion;

    /// <summary>
    /// The configuration directory for the current Revit version under the user's Documents folder.
    /// </summary>
    public static string Directory => _defaultPaths.Directory;

    /// <summary>Gets the top-level user configuration file path for the current Revit version.</summary>
    public static string ConfigPath => _defaultPaths.ConfigPath;

    /// <summary>Gets the example configuration file path for the current Revit version.</summary>
    public static string ExampleConfigPath => _defaultPaths.ExampleConfigPath;

    /// <summary>Gets the debug configuration file path for the current Revit version.</summary>
    public static string DebugConfigPath => _defaultPaths.DebugConfigPath;

    /// <summary>Gets the managed practical-tour configuration file path.</summary>
    public static string PracticalTourConfigPath => _defaultPaths.PracticalTourConfigPath;

    /// <summary>Gets the legacy split configuration files of one Revit version folder.</summary>
    /// <param name="versionDirectory">The configuration folder of one Revit version.</param>
    /// <returns>The top-level legacy files plus their copies in the <c>examples</c> and <c>debug</c> folders.</returns>
    /// <remarks>
    /// The startup migration moves these files into the <c>legacy</c> subfolder of the same version folder; the
    /// product does not read them afterwards. The files that do not exist are simply absent from the result of
    /// the caller's existence check.
    /// </remarks>
    public static IReadOnlyList<string> GetLegacyConfigurationPaths(string versionDirectory) =>
    [
        Path.Combine(versionDirectory, LegacyConfigFileName),
        Path.Combine(versionDirectory, LegacyCollisionConfigFileName),
        Path.Combine(versionDirectory, LegacyParameterElementConfigFileName),
        Path.Combine(versionDirectory, ExamplesFolderName, LegacyConfigFileName),
        Path.Combine(versionDirectory, ExamplesFolderName, LegacyCollisionConfigFileName),
        Path.Combine(versionDirectory, ExamplesFolderName, LegacyParameterElementConfigFileName),
        Path.Combine(versionDirectory, DebugFolderName, LegacyConfigFileName),
        Path.Combine(versionDirectory, DebugFolderName, LegacyCollisionConfigFileName),
        Path.Combine(versionDirectory, DebugFolderName, LegacyParameterElementConfigFileName),
    ];

    /// <summary>
    /// Gets the header written into a newly created configuration file.
    /// </summary>
    /// <remarks>
    /// The text is owned by the shared configuration format, so the add-in and the diagnostics editor
    /// create identical files. It explains the format to a user who opens the file for the first time,
    /// so the product never creates an unexplained empty file.
    /// </remarks>
    public static string ConfigurationHeader => DiagnosticConfigurationFormat.Header;

    /// <summary>Gets the diagnostic configuration file format version this product reads.</summary>
    /// <remarks>
    /// Configuration files are versioned so that an older product never tries to interpret a file written
    /// for a newer one. Version 2 is the single-file format with a per-rule <c>type</c> discriminator.
    /// </remarks>
    public const int SupportedConfigurationVersion = DiagnosticConfigurationFormat.SupportedVersion;

    /// <summary>
    /// Creates the parent directory and a configuration file with the format header when the file does not exist.
    /// </summary>
    /// <param name="path">The configuration file path.</param>
    /// <exception cref="T:System.InvalidOperationException"><paramref name="path"/> does not contain a directory.</exception>
    public static void EnsureConfigurationFile(string path)
    {
        string? directoryPath = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Diagnostic configuration file not found.");
        System.IO.Directory.CreateDirectory(directoryPath);

        if (!File.Exists(path))
            File.WriteAllText(path, ConfigurationHeader);
    }

    /// <summary>
    /// Reads a diagnostic configuration file with its format metadata.
    /// </summary>
    /// <param name="path">The configuration file path.</param>
    /// <returns>The parsed file, or <see langword="null"/> when the file is missing or empty.</returns>
    /// <remarks>
    /// The format version is returned as written; the caller decides whether this product supports it.
    /// A file that is not valid YAML throws <see cref="T:YamlDotNet.Core.YamlException"/>.
    /// </remarks>
    public static DiagnosticConfigurationFile? GetDiagnosticConfiguration(string path) =>
        DiagnosticConfigurationReader.ReadFile(path);

    /// <summary>
    /// Attempts to read a diagnostic configuration file and converts YAML parsing failures into a failed result.
    /// </summary>
    /// <param name="path">The configuration file path.</param>
    /// <param name="configuration">The parsed file, or <see langword="null"/> when the file is missing or empty.</param>
    /// <param name="error">The YAML parsing error when reading failed; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the file was read successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetDiagnosticConfiguration(
        string path,
        out DiagnosticConfigurationFile? configuration,
        out Exception? error) =>
        DiagnosticConfigurationReader.TryReadFile(path, out configuration, out error);
}
