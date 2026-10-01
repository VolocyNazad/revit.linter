namespace Revit.Linter.Updater.Core.Models;

/// <summary>Describes a stable downloadable product release.</summary>
public sealed record ReleaseInfo(StableVersion Version, Uri ReleasePage);
