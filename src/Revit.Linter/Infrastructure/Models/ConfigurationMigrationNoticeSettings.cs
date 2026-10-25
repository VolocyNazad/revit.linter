using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Infrastructure.Models;

/// <summary>Stores the Revit versions for which the configuration migration notice was already shown.</summary>
[StoreFile("configurationMigrationNotice.yml")]
public sealed class ConfigurationMigrationNoticeSettings
{
    /// <summary>Gets or sets the Revit years for which the migration notice was shown.</summary>
    public List<int> NotifiedRevitVersions { get; set; } = [];
}
