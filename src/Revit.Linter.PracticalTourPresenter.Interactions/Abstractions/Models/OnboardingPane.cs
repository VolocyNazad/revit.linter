namespace Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;

/// <summary>Identifies a Revit Linter pane that the onboarding experience may bring to the foreground.</summary>
public enum OnboardingPane
{
    /// <summary>The pane used to select and run diagnostics.</summary>
    Diagnostics,

    /// <summary>The pane that presents diagnostic findings.</summary>
    Warnings,

    /// <summary>The pane that presents applied fixes.</summary>
    FixList,

    /// <summary>The pane that presents ignored findings.</summary>
    IgnoreList,
}
