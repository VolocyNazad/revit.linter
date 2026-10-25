namespace Revit.Linter.ConfigurationPath.Abstractions.Services;

/// <summary>Locates the diagnostic configuration files for the running Revit version.</summary>
/// <remarks>
/// The path set is injected instead of read from static state, so a test can create the same layout in a
/// temporary directory and a host can resolve the paths for the version it targets.
/// </remarks>
public interface IConfigurationPaths
{
    /// <summary>Gets the Revit release year this build targets, for example <c>2025</c>.</summary>
    int RevitVersion { get; }

    /// <summary>Gets the configuration directory of the current Revit version.</summary>
    string Directory { get; }

    /// <summary>Gets the top-level user configuration file path.</summary>
    string ConfigPath { get; }

    /// <summary>Gets the example configuration file path.</summary>
    string ExampleConfigPath { get; }

    /// <summary>Gets the debug configuration file path.</summary>
    string DebugConfigPath { get; }

    /// <summary>Gets the managed practical-tour configuration file path.</summary>
    string PracticalTourConfigPath { get; }

    /// <summary>Gets the folder that contains one configuration folder per Revit version.</summary>
    /// <remarks>
    /// The startup migration uses it to move the legacy files of every Revit version at once, not only the
    /// version that is running.
    /// </remarks>
    string VersionsDirectory { get; }
}
