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
    public required string Description { get; init; } = string.Empty;
    /// <summary>Gets the report message template. A missing value falls back to the localized module default.</summary>
    public string? Message { get; init; }
    public DiagnosticSeverity Severity { get; init; } = DiagnosticSeverity.Message;
    public bool IsActive { get; init; } = true;
    public bool IsObsolete { get; init; } = false;
    public string ObsoleteDescription { get; init; } = string.Empty;
    public ElementFixPipelineDefinition[] Fixes { get; init; } = [];
    public ElementVisualizationPipelineDefinition[] Visualizations { get; init; } =
        [new() { Name = "Show and select", Steps = [new() { Type = "Show" }, new() { Type = "Select" }] }];
}
