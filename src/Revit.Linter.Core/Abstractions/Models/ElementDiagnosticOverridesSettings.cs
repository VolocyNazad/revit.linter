using Revit.Linter.ValueStore.Abstractions;

namespace Revit.Linter.Core.Abstractions.Models;

[StoreFile("elementDiagnosticSettings.yml")]
public sealed class ElementDiagnosticOverridesSettings
{
    public Dictionary<string, DiagnosticOverrideSettings> Overrides { get; set; } = [];
}
