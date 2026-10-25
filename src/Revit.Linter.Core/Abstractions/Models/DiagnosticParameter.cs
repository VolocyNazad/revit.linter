namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Describes one project parameter required by a parameter diagnostic rule.</summary>
/// <remarks>
/// The properties are nullable because YAML deserialization does not enforce required members; the
/// parameter rule validator reports the missing values before the rule is registered.
/// </remarks>
public sealed class DiagnosticParameter
{
    /// <summary>Gets the parameter name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the shared parameter GUID.</summary>
    public string? Guid { get; init; }

    /// <summary>Gets whether the parameter is bound as an instance parameter.</summary>
    public bool IsInstance { get; init; }

    /// <summary>Gets the category identifiers the parameter is bound to.</summary>
    public List<string>? Categories { get; init; }

    /// <summary>Gets whether the parameter may vary between groups.</summary>
    public bool AllowVaryBetweenGroups { get; init; }

    /// <summary>Gets the parameter group identifier.</summary>
    public string? Group { get; init; }
}
