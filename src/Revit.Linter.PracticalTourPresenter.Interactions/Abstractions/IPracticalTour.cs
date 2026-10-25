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

    /// <summary>Replaces the managed practical-tour configuration when it is not in the current format.</summary>
    /// <remarks>
    /// The file is owned by the product, so an outdated copy from a previous product version is refreshed
    /// instead of being reported as a broken user configuration: without this, the old file would raise a
    /// configuration error at startup and the training diagnostic would be missing until the tour starts
    /// again. An active tour session keeps its file. A failure is logged and never affects the add-in.
    /// </remarks>
    void RefreshOutdatedConfiguration();
}
