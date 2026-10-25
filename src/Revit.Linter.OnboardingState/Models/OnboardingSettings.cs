using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.OnboardingState.Models;

/// <summary>Stores which parts of the onboarding experience the current user has already seen.</summary>
/// <remarks>The store file keeps its original name so existing user settings remain valid.</remarks>
[StoreFile("welcomeSettings.yml")]
public sealed class OnboardingSettings
{
    /// <summary>Gets or sets the version of the wizard the user has seen; <c>0</c> means never.</summary>
    public int CompletedWizardVersion { get; set; }

    /// <summary>Gets or sets the Revit release years for which the examples step was already offered.</summary>
    /// <remarks>The configuration folder is separate for every Revit version, so the offer is too.</remarks>
    public List<int> ExamplesOfferedRevitVersions { get; set; } = [];

    /// <summary>Gets or sets independent practical-tour progress for each Revit release year.</summary>
    public Dictionary<int, PracticalTourProgress> PracticalTourProgressByRevitVersion { get; set; } = [];
}
