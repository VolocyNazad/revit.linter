namespace Revit.Linter.Updater.Core.Models;

/// <summary>Contains the outcome and optional release data for one update check.</summary>
public sealed record UpdateCheckResult(
    UpdateCheckStatus Status,
    StableVersion CurrentVersion,
    ReleaseInfo? Release = null,
    string? Error = null);
