namespace Revit.Linter.WelcomePresenter.Abstractions.Models;

/// <summary>Stores resumable practical-tour state for one Revit release.</summary>
public sealed class PracticalTourProgress
{
    /// <summary>Gets or sets the next step the user is expected to complete.</summary>
    public PracticalTourStep CurrentStep { get; set; } = PracticalTourStep.OpenDocument;

    /// <summary>Gets or sets whether finding-only steps were skipped after a clean diagnostic run.</summary>
    public bool HasCleanRun { get; set; }

    /// <summary>Gets or sets whether the inspected finding offered only one visualization.</summary>
    public bool HasSingleVisualization { get; set; }

    /// <summary>Gets or sets whether the user completed every applicable step.</summary>
    public bool IsCompleted { get; set; }

    /// <summary>Gets or sets whether the user explicitly opted out of the practical tour.</summary>
    public bool IsOptedOut { get; set; }
}
