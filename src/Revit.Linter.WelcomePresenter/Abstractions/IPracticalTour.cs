namespace Revit.Linter.WelcomePresenter.Abstractions;

/// <summary>Controls the optional modeless practical-tour session.</summary>
public interface IPracticalTour
{
    /// <summary>Starts or resumes the tour for the current document state.</summary>
    /// <param name="hasOpenDocument">Whether a Revit document is currently available.</param>
    /// <param name="restart">Whether saved completion, opt-out and progress should be discarded first.</param>
    /// <param name="tutorialSampleQueued">Whether the open-document step should direct the user to the dedicated ribbon command.</param>
    void Start(bool hasOpenDocument, bool restart = false, bool tutorialSampleQueued = false);

    /// <summary>Clears saved practical-tour progress for the running Revit release.</summary>
    void ResetProgress();
}
