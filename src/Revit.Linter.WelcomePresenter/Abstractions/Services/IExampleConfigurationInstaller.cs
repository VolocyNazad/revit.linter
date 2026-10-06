using Revit.Linter.WelcomePresenter.Abstractions.Models;

namespace Revit.Linter.WelcomePresenter.Abstractions.Services;

/// <summary>
/// Installs the example diagnostic configurations into the configuration folder of the running Revit version.
/// </summary>
public interface IExampleConfigurationInstaller
{
    /// <summary>Gets the configuration folder the examples are installed into.</summary>
    string TargetDirectory { get; }

    /// <summary>
    /// Writes the example configuration files that contain the blocks of the selected disciplines.
    /// </summary>
    /// <param name="disciplines">The disciplines whose blocks are included; must not be empty.</param>
    /// <returns>The files that were written.</returns>
    /// <exception cref="System.ArgumentException"><paramref name="disciplines"/> is empty.</exception>
    /// <remarks>
    /// A missing or empty configuration file is replaced by the example. A non-empty file is never
    /// overwritten: the example is written to the <c>examples</c> subfolder instead, replacing an example
    /// installed there earlier. Message texts follow the current UI culture, and parameter groups are
    /// written in the format of the running Revit version. File-system failures are not caught.
    /// </remarks>
    ExampleInstallationResult Install(IReadOnlyCollection<ExampleDiscipline> disciplines);

    /// <summary>Installs the managed diagnostic used by the practical tour.</summary>
    /// <returns>The full path of the installed configuration file.</returns>
    /// <remarks>
    /// The file is owned by Revit Linter and is refreshed on every tour start. It is stored in the
    /// <c>tour</c> subfolder and does not overwrite the user's main configuration.
    /// </remarks>
    string InstallPracticalTour();

    /// <summary>Removes the managed diagnostic used by the practical tour, if it is installed.</summary>
    /// <remarks>
    /// Called when the tour is completed or dismissed, so no training configuration is left behind.
    /// Starting the tour again reinstalls the file.
    /// </remarks>
    void RemovePracticalTour();
}
