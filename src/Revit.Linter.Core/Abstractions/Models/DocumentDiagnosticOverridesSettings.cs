using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Core.Abstractions.Models;

[StoreFile("documentDiagnosticSettings.yml")]
public sealed class DocumentDiagnosticOverridesSettings
{
    public Dictionary<string, DiagnosticOverrideSettings> Overrides { get; set; } = [];
}
