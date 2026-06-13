namespace Revit.Linter.ParameterElementDiagnostics.Models;

internal sealed class ParameterElementData
{
    public required string Name { get; init; }
    public required string Guid { get; init; }
    public required bool IsInstance { get; init; }
    public required List<string> Categories { get; init; }
    public required bool AllowVaryBetweenGroups { get; init; }
    public required string Group { get; init; }
}

