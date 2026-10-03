using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.WelcomePresenter.Abstractions.Models;

/// <summary>Stores which parts of the welcome wizard the current user has already seen.</summary>
[StoreFile("welcomeSettings.yml")]
public sealed class WelcomeSettings
{
    /// <summary>Gets or sets the version of the wizard the user has seen; <c>0</c> means never.</summary>
    public int CompletedWizardVersion { get; set; }

    /// <summary>Gets or sets the Revit release years for which the examples step was already offered.</summary>
    /// <remarks>The configuration folder is separate for every Revit version, so the offer is too.</remarks>
    public List<int> ExamplesOfferedRevitVersions { get; set; } = [];
}
