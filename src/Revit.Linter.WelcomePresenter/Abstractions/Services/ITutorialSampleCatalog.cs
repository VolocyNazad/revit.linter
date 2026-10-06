using Revit.Linter.WelcomePresenter.Abstractions.Models;

namespace Revit.Linter.WelcomePresenter.Abstractions.Services;

/// <summary>Finds compatible official Autodesk samples for the currently running Revit installation.</summary>
public interface ITutorialSampleCatalog
{
    /// <summary>
    /// Returns compatible project samples from the running Revit installation, ordered for the selected disciplines.
    /// </summary>
    /// <param name="preferredDisciplines">The disciplines selected in the welcome window.</param>
    /// <remarks>
    /// Implementations must inspect only the official Samples directory of the current Revit installation. Discovery
    /// failures produce an empty collection and must not affect add-in startup or ordinary document operations.
    /// </remarks>
    IReadOnlyList<TutorialSampleCandidate> Find(IReadOnlyCollection<ExampleDiscipline> preferredDisciplines);
}
