namespace Revit.Linter.WelcomePresenter.Abstractions.Models;

/// <summary>Identifies a Revit Linter pane that the welcome experience may bring to the foreground.</summary>
public enum WelcomePane
{
    /// <summary>The pane used to select and run diagnostics.</summary>
    Diagnostics,

    /// <summary>The pane that presents diagnostic findings.</summary>
    Warnings,

    /// <summary>The pane that presents applied fixes.</summary>
    FixList,
}
