using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Stores user overrides keyed by document diagnostic code.</summary>
[StoreFile("documentDiagnosticSettings.yml")]
public sealed class DocumentDiagnosticOverridesSettings
{
    /// <summary>Gets or sets the overrides keyed by stable diagnostic code.</summary>
    public Dictionary<string, DiagnosticOverrideSettings> Overrides { get; set; } = [];
}
