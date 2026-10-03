using Revit.Linter.WelcomePresenter.Abstractions.Models;

namespace Revit.Linter.WelcomePresenter.Infrastructure.Services;

/// <summary>
/// Names the optional wizard steps that one showing of the wizard includes; the final step is always shown.
/// </summary>
/// <param name="IncludeIntro">Whether the introduction step is shown.</param>
/// <param name="IncludeExamples">Whether the examples step is shown.</param>
internal readonly record struct WelcomeWizardPlan(bool IncludeIntro, bool IncludeExamples)
{
    /// <summary>Gets the plan that shows every step.</summary>
    public static WelcomeWizardPlan AllSteps => new(IncludeIntro: true, IncludeExamples: true);

    /// <summary>Gets a value indicating whether the plan contains a step the user has not seen yet.</summary>
    public bool HasSteps => IncludeIntro || IncludeExamples;
}

/// <summary>
/// Decides which wizard steps are still pending for a user and records the steps that were shown.
/// </summary>
/// <remarks>Free of Revit and WPF types so that the first-run rules are covered by headless tests.</remarks>
internal static class WelcomeWizardState
{
    /// <summary>
    /// Returns the steps the user has not seen: the introduction until the current wizard version is
    /// completed, and the examples until they were offered for the given Revit version.
    /// </summary>
    public static WelcomeWizardPlan GetPendingPlan(WelcomeSettings settings, int currentWizardVersion, int revitVersion) =>
        new(
            IncludeIntro: settings.CompletedWizardVersion < currentWizardVersion,
            IncludeExamples: !settings.ExamplesOfferedRevitVersions.Contains(revitVersion));

    /// <summary>
    /// Records the shown steps. The completed version never decreases and a Revit version is stored once.
    /// </summary>
    public static void MarkShown(
        WelcomeSettings settings, WelcomeWizardPlan plan, int currentWizardVersion, int revitVersion)
    {
        if (plan.IncludeIntro && settings.CompletedWizardVersion < currentWizardVersion)
            settings.CompletedWizardVersion = currentWizardVersion;
        if (plan.IncludeExamples && !settings.ExamplesOfferedRevitVersions.Contains(revitVersion))
            settings.ExamplesOfferedRevitVersions.Add(revitVersion);
    }
}
