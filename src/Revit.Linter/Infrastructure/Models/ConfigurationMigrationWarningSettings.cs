using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Infrastructure.Models;

/// <summary>Stores whether the configuration migration warning was shown and for which product version.</summary>
[StoreFile("configurationMigrationWarning.yml")]
public sealed class ConfigurationMigrationWarningSettings
{
    /// <summary>Gets or sets the informational product version that showed the warning.</summary>
    /// <remarks>
    /// The warning is shown once per product version. <see langword="null"/> means that no warning was shown
    /// yet, for example after a settings reset or on a fresh installation.
    /// </remarks>
    public string? NotifiedVersion { get; set; }

    /// <summary>Gets or sets whether legacy files were moved but the warning has not been displayed yet.</summary>
    /// <remarks>
    /// The flag survives a restart: Revit may close while the welcome wizard is open and the queued warning never
    /// runs, and on the next start the files are already in the legacy folder, so the move itself cannot trigger
    /// the warning again. It is cleared together with recording <see cref="NotifiedVersion"/>.
    /// </remarks>
    public bool PendingWarning { get; set; }
}
