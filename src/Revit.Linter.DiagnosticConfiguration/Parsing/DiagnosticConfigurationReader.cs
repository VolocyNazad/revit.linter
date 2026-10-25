using Revit.Linter.DiagnosticConfiguration.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Revit.Linter.DiagnosticConfiguration.Parsing;

/// <summary>Reads a diagnostic configuration file into its envelope and rule list.</summary>
/// <remarks>
/// The reader performs no semantic check. It returns <see langword="null"/> for a missing or empty file
/// and lets <see cref="YamlException"/> escape for a file that is not valid YAML, so the caller decides
/// whether a read failure is an error to report or a reason to skip a source.
/// </remarks>
public static class DiagnosticConfigurationReader
{
    // DeserializerBuilder builds the type graph via reflection: build once and reuse.
    private static readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    /// <summary>Reads configuration text.</summary>
    /// <param name="yaml">The configuration file content.</param>
    /// <returns>The parsed file, or <see langword="null"/> when the text is empty.</returns>
    /// <exception cref="YamlException">The text is not valid YAML.</exception>
    public static DiagnosticConfigurationFile? Read(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml)) return null;
        return _deserializer.Deserialize<DiagnosticConfigurationFile>(yaml);
    }

    /// <summary>Reads one configuration file from disk.</summary>
    /// <param name="path">The configuration file path.</param>
    /// <returns>The parsed file, or <see langword="null"/> when the file is missing or empty.</returns>
    /// <exception cref="YamlException">The file is not valid YAML.</exception>
    public static DiagnosticConfigurationFile? ReadFile(string path)
    {
        if (!File.Exists(path)) return null;
        return Read(File.ReadAllText(path));
    }

    /// <summary>Attempts to read configuration text and converts YAML parsing failures into a failed result.</summary>
    /// <param name="yaml">The configuration file content.</param>
    /// <param name="configuration">The parsed file, or <see langword="null"/> when the text is empty.</param>
    /// <param name="error">The YAML parsing error when reading failed; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the text was read successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryRead(
        string yaml,
        out DiagnosticConfigurationFile? configuration,
        out Exception? error)
    {
        try
        {
            configuration = Read(yaml);
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

    /// <summary>Attempts to read one configuration file and converts YAML parsing failures into a failed result.</summary>
    /// <param name="path">The configuration file path.</param>
    /// <param name="configuration">The parsed file, or <see langword="null"/> when the file is missing or empty.</param>
    /// <param name="error">The YAML parsing error when reading failed; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the file was read successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryReadFile(
        string path,
        out DiagnosticConfigurationFile? configuration,
        out Exception? error)
    {
        try
        {
            configuration = ReadFile(path);
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
