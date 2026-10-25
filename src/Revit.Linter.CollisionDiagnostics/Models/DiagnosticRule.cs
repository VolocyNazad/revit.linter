namespace Revit.Linter.CollisionDiagnostics.Models;

internal sealed class DiagnosticRule
{
    public required string Code { get; init; }
    public required string TakeDocument { get; init; }
    public required string Take { get; init; }
    public required string AndTake { get; init; }
    public required string GroupBy { get; init; }
    public bool IncludeLinks { get; init; } = false;
    /// <summary>Gets whether elements joined by MEP connectors are excluded from collision checks.</summary>
    /// <remarks>Joined elements touch by design, while their family geometry overlaps at the joint and reports a false volumetric collision. A joined pair that genuinely collides elsewhere is skipped as well.</remarks>
    public bool IgnoreConnectedElements { get; init; } = false;
    /// <summary>Gets whether elements that belong to one placed family are excluded from collision checks.</summary>
    /// <remarks>A parent instance and its nested families, and nested families of the same parent instance, are one placed family: their overlap is decided inside the family, so a pair from the same family tree is skipped even when it genuinely intersects.</remarks>
    public bool IgnoreNestedFamilies { get; init; } = false;
    public required string Description { get; init; } = string.Empty;
    /// <summary>Gets the report message template. A missing value falls back to the localized module default.</summary>
    public string? Message { get; init; }
    public DiagnosticSeverity Severity { get; init; } = DiagnosticSeverity.Message;
    public bool IsActive { get; init; } = true;
    public bool IsObsolete { get; init; } = false;
    public string ObsoleteDescription { get; init; } = string.Empty;
    /// <summary>Gets the tags used to find this rule; an empty list when none are declared.</summary>
    public string[] Tags { get; init; } = [];
    public ElementFixPipelineDefinition[] Fixes { get; init; } = [];
    public ElementVisualizationPipelineDefinition[] Visualizations { get; init; } =
        [new() { Name = "Show and select", Steps = [new() { Type = "Show" }, new() { Type = "Select" }] }];
}
