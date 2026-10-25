namespace Revit.Linter.DiagnosticConfiguration.Editing;

/// <summary>Describes the scalar fields one rule receives in a single edit pass.</summary>
/// <param name="Location">The rule to edit.</param>
/// <param name="Values">The field keys and their replacement values exactly as YAML text.</param>
public readonly record struct DiagnosticRuleScalarUpdate(
    DiagnosticRuleLocation Location,
    IReadOnlyList<KeyValuePair<string, string>> Values);
