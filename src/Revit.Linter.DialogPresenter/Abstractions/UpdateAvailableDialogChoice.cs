namespace Revit.Linter.DialogPresenter.Abstractions;

/// <summary>Identifies the decision the user made in the update-available dialog.</summary>
public enum UpdateAvailableDialogChoice
{
    /// <summary>The dialog was closed without a decision; the next Revit start may ask again.</summary>
    Close,

    /// <summary>The user asked not to be offered this release until a newer one is published.</summary>
    SkipVersion,

    /// <summary>The user asked to open the GitHub release page.</summary>
    OpenRelease
}
