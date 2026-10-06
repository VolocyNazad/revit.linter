using Revit.Linter.Core.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Core;

namespace Revit.Linter.ConfigurationPath;

/// <summary>
/// Locates, creates, and reads the version-specific Revit Linter configuration files.
/// </summary>
public static class ConfigurationPathUtils
{
    /// <summary>Gets the user configuration file name shared by element diagnostics.</summary>
    public const string ConfigFileName = "config.yaml";

    /// <summary>Gets the user configuration file name for collision diagnostics.</summary>
    public const string CollisionConfigFileName = "collision.config.yaml";

    /// <summary>Gets the user configuration file name for project-parameter diagnostics.</summary>
    public const string ParameterElementConfigFileName = "parameter-element.config.yaml";

    /// <summary>Gets the managed practical-tour configuration file name.</summary>
    public const string PracticalTourFileName = "practical-tour.config.yaml";

    /// <summary>Gets the subfolder that receives examples without overwriting user files.</summary>
    public const string ExamplesFolderName = "examples";

    /// <summary>Gets the subfolder that holds the managed practical-tour configuration.</summary>
    public const string TourFolderName = "tour";
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

    /// <summary>
    /// Gets the Revit release year this build targets, for example <c>2025</c>.
    /// </summary>
    public static int RevitVersion => _revitVersion;

    /// <summary>
    /// The configuration directory for the current Revit version under the user's Documents folder.
    /// </summary>
    public static readonly string Directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        ProductIdentity.DocumentsFolderName,
        _revitVersion.ToString()
    );

    /// <summary>Gets the main user configuration file path for the current Revit version.</summary>
    public static string ConfigPath => Path.Combine(Directory, ConfigFileName);

    /// <summary>Gets the example user configuration file path for the current Revit version.</summary>
    public static string ExampleConfigPath => Path.Combine(Directory, ExamplesFolderName, ConfigFileName);

    /// <summary>Gets the managed practical-tour configuration file path.</summary>
    public static string PracticalTourConfigPath =>
        Path.Combine(Directory, TourFolderName, PracticalTourFileName);

    /// <summary>Gets the collision configuration file path for the current Revit version.</summary>
    public static string CollisionConfigPath => Path.Combine(Directory, CollisionConfigFileName);

    /// <summary>Gets the example collision configuration file path.</summary>
    public static string ExampleCollisionConfigPath =>
        Path.Combine(Directory, ExamplesFolderName, CollisionConfigFileName);

    /// <summary>Gets the project-parameter configuration file path.</summary>
    public static string ParameterElementConfigPath => Path.Combine(Directory, ParameterElementConfigFileName);

    /// <summary>Gets the example project-parameter configuration file path.</summary>
    public static string ExampleParameterElementConfigPath =>
        Path.Combine(Directory, ExamplesFolderName, ParameterElementConfigFileName);

    /// <summary>
    /// Creates the parent directory and an empty file when the specified configuration file does not exist.
    /// </summary>
    /// <param name="path">The configuration file path.</param>
    /// <exception cref="InvalidOperationException"><paramref name="path"/> does not contain a directory.</exception>
    public static void EnsureFileExists(string path)
    {
        string? directoryPath = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Diagnostic configuration file not found.");
        System.IO.Directory.CreateDirectory(directoryPath);

        if (!File.Exists(path))
            File.WriteAllText(path, string.Empty);
    }

    /// <summary>
    /// Reads and deserializes a YAML configuration file using camel-case member names.
    /// </summary>
    /// <typeparam name="T">The reference type represented by the configuration file.</typeparam>
    /// <param name="configPath">The configuration file path.</param>
    /// <returns>The deserialized configuration, or <see langword="null"/> when the file is empty.</returns>
    /// <remarks>A missing file is created before it is read.</remarks>
    public static T? GetConfigurations<T>(string configPath) where T : class
    {
        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        EnsureFileExists(configPath);
        string configContent = File.ReadAllText(configPath);
        if (string.IsNullOrEmpty(configContent)) return null;
        T? rules = deserializer.Deserialize<T>(configContent);
        return rules;
    }

    /// <summary>
    /// Attempts to read a YAML configuration and converts YAML parsing failures into a failed result.
    /// </summary>
    /// <typeparam name="T">The reference type represented by the configuration file.</typeparam>
    /// <param name="configPath">The configuration file path.</param>
    /// <param name="configuration">The deserialized configuration, or <see langword="null"/> when the file is empty or invalid.</param>
    /// <param name="error">The YAML parsing error when reading failed; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the file was read successfully; otherwise <see langword="false"/>.</returns>
    public static bool TryGetConfigurations<T>(
        string configPath, out T? configuration, out Exception? error) where T : class
    {
        try
        {
            configuration = GetConfigurations<T>(configPath);
            error = null;
            return true;
        }
        catch (YamlException exception)
        {
            configuration = null;
            error = exception;
            return false;
        }
    }
}
