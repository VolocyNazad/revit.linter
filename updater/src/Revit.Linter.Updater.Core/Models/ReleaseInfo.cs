namespace Revit.Linter.Updater.Core.Models;

/// <summary>Describes a stable product release and its optional validated installer asset.</summary>
public sealed record ReleaseInfo(
    StableVersion Version,
    Uri ReleasePage,
    ReleaseInstaller? Installer = null);
