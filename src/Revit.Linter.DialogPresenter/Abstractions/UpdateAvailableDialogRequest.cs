namespace Revit.Linter.DialogPresenter.Abstractions;

/// <summary>
/// Describes the available release shown by the update-available dialog.
/// </summary>
/// <param name="Version">The stable version of the available release.</param>
/// <param name="ReleasePage">The absolute address of the GitHub release page.</param>
public sealed record UpdateAvailableDialogRequest(string Version, string ReleasePage);
