using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Infrastructure.Models;

/// <summary>Stores the product version for which the configuration migration warning was shown.</summary>
[StoreFile("configurationMigrationWarning.yml")]
public sealed class ConfigurationMigrationWarningSettings
{
    /// <summary>Gets or sets the informational product version that showed the warning.</summary>
    /// <remarks>
    /// The warning is shown once per product version. <see langword="null"/> means that no warning was shown
    /// yet, for example after a settings reset or on a fresh installation.
    /// </remarks>
    public string? NotifiedVersion { get; set; }
}
