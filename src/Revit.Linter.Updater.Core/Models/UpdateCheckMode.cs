namespace Revit.Linter.Updater.Core.Models;

/// <summary>Identifies why an update check was requested.</summary>
public enum UpdateCheckMode
{
    /// <summary>A scheduled background check subject to user settings and the check interval.</summary>
    Automatic,

    /// <summary>An explicit user request that bypasses the automatic check interval.</summary>
    Manual
}
