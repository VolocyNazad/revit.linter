namespace Revit.Linter.ConfigurationPath.Abstractions.Models;

/// <summary>Describes one configuration file location and its provenance.</summary>
/// <param name="Path">The configuration file path.</param>
/// <param name="IsExample">Whether the file is an installed example configuration.</param>
/// <param name="IsDebug">Whether the file is a debug configuration.</param>
/// <param name="IsTour">Whether the file is the managed practical-tour configuration.</param>
/// <param name="IsTopLevel">Whether the file is the user's top-level configuration, which is created when missing.</param>
public sealed record DiagnosticConfigurationFileDescriptor(
    string Path,
    bool IsExample = false,
    bool IsDebug = false,
    bool IsTour = false,
    bool IsTopLevel = false);
