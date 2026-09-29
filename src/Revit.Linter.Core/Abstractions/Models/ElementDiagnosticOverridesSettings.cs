using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Stores user overrides keyed by element diagnostic code.</summary>
[StoreFile("elementDiagnosticSettings.yml")]
public sealed class ElementDiagnosticOverridesSettings
{
    /// <summary>Gets or sets the overrides keyed by stable diagnostic code.</summary>
    public Dictionary<string, DiagnosticOverrideSettings> Overrides { get; set; } = [];
}
