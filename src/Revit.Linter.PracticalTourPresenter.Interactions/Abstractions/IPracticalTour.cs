namespace Revit.Linter.PracticalTourPresenter.Interactions.Abstractions;

/// <summary>Controls the optional modeless practical-tour session.</summary>
public interface IPracticalTour
{
    /// <summary>Starts or resumes the tour for the current document state.</summary>
    /// <param name="hasOpenDocument">Whether a Revit document is currently available.</param>
    /// <param name="restart">Whether saved completion, opt-out and progress should be discarded first.</param>
    /// <param name="tutorialSampleQueued">Whether the open-document step should direct the user to the dedicated ribbon command.</param>
    /// <remarks>
    /// A saved completed or opted-out state is always discarded: every start carries an
    /// explicit user intent to tour again, so the tour starts over instead of staying hidden.
    /// </remarks>
    void Start(bool hasOpenDocument, bool restart = false, bool tutorialSampleQueued = false);

    /// <summary>Clears saved practical-tour progress for the running Revit release.</summary>
    void ResetProgress();

    /// <summary>Hides the practical-tour pane when no tour session is active.</summary>
    /// <remarks>
    /// Revit restores pane visibility from the previous session when a document opens, while a fresh
    /// process has no tour view-model: without this call a restored pane stays visible with no content.
    /// An active session keeps its pane; use <see cref="Start"/> to show it.
    /// </remarks>
    void HidePaneIfInactive();
}
