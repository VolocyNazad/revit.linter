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

    /// <summary>
    /// Writes the example configuration that contains the selected discipline blocks and template check blocks.
    /// </summary>
    /// <param name="disciplines">The example disciplines whose blocks are included; may be empty.</param>
    /// <param name="packs">The template check packs whose blocks are appended; may be empty.</param>
    /// <returns>The files that were written.</returns>
    /// <exception cref="System.ArgumentException">Both collections are empty.</exception>
    /// <remarks>
    /// Both selections are composed into the single installed example file. The check blocks are appended
    /// after the example rules, so installing checks alone still keeps the element checks of the example in
    /// place. Message texts follow the current UI culture. The block edition is chosen from the running
    /// Revit version: the <c>PG_*</c> blocks through Revit 2023 and the ForgeTypeId blocks from Revit 2024.
    /// </remarks>
    ExampleInstallationResult Install(
        IReadOnlyCollection<ExampleDiscipline> disciplines, IReadOnlyCollection<TemplateCheckPack> packs);
}
