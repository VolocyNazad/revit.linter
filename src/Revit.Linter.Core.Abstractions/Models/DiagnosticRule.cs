namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>
/// Describes one entry of the diagnostic configuration file.
/// </summary>
/// <remarks>
/// Every property is optional at the deserialization level so that one malformed rule can be described
/// and skipped without losing the rest of the file. Which properties are required depends on
/// <see cref="Type"/>; the configuration loader validates the combinations and reports invalid rules
/// through the configuration error source. A <see cref="DiagnosticRuleType.BuiltIn"/> entry carries only
/// <see cref="Code"/>, <see cref="IsActive"/>, <see cref="Severity"/> and <see cref="IsHidden"/>.
/// </remarks>
public sealed class DiagnosticRule
{
    /// <summary>Gets the configuration spelling of the rule type: <c>custom</c>, <c>collision</c>, <c>parameter</c> or <c>builtIn</c>.</summary>
    public string? Type { get; init; }

    /// <summary>Gets the stable user-facing diagnostic code.</summary>
    public string? Code { get; init; }

    /// <summary>Gets the diagnostic description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the report message template; a missing value falls back to the module default.</summary>
    public string? Message { get; init; }

    /// <summary>Gets the effective severity, or <see langword="null"/> to use the default.</summary>
    public DiagnosticSeverity? Severity { get; init; }

    /// <summary>Gets whether the diagnostic participates in execution, or <see langword="null"/> to use the default.</summary>
    public bool? IsActive { get; init; }

    /// <summary>Gets whether the diagnostic is hidden from the interface, execution and reports.</summary>
    public bool? IsHidden { get; init; }

    /// <summary>Gets whether the diagnostic is retained only for compatibility.</summary>
    public bool? IsObsolete { get; init; }

    /// <summary>Gets the migration guidance for an obsolete diagnostic.</summary>
    public string? ObsoleteDescription { get; init; }

    /// <summary>Gets the user tags used to find the diagnostic.</summary>
    public string[] Tags { get; init; } = [];

    /// <summary>Gets the document filter formula of an element or parameter rule.</summary>
    public string? TakeDocument { get; init; }

    /// <summary>Gets the element filter formula of a rule.</summary>
    public string? Take { get; init; }

    /// <summary>Gets the user element diagnostic formula.</summary>
    public string? Check { get; init; }

    /// <summary>Gets the collision rule formula for the second element set.</summary>
    public string? AndTake { get; init; }

    /// <summary>Gets the collision grouping formula.</summary>
    public string? GroupBy { get; init; }

    /// <summary>Gets whether a collision rule also examines loaded linked models.</summary>
    public bool? IncludeLinks { get; init; }

    /// <summary>Gets whether a collision rule skips elements joined by connectors.</summary>
    public bool? IgnoreConnectedElements { get; init; }

    /// <summary>Gets whether a collision rule skips two elements that belong to one placed family.</summary>
    /// <remarks>
    /// A parent instance and its nested families, and nested families of the same parent instance,
    /// form one family tree: their overlap is decided inside the family, so the pair is not reported.
    /// The relation comes from <c>FamilyInstance.SuperComponent</c> and therefore does not depend on
    /// the rule's formulas or on whether the parent itself is selected.
    /// </remarks>
    public bool? IgnoreNestedFamilies { get; init; }

    /// <summary>Gets the project parameters of a parameter rule.</summary>
    public DiagnosticParameter[]? Parameters { get; init; }

    /// <summary>Gets the fix pipelines declared by the rule.</summary>
    public ElementFixPipelineDefinition[] Fixes { get; init; } = [];

    /// <summary>Gets the visualization pipelines declared by the rule.</summary>
    public ElementVisualizationPipelineDefinition[] Visualizations { get; init; } = [];
}
