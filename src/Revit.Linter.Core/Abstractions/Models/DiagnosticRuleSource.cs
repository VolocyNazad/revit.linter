namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Associates one configuration rule with the file and provenance it was loaded from.</summary>
/// <param name="Rule">The configuration entry.</param>
/// <param name="Path">The configuration file path.</param>
/// <param name="IsExample">Whether the rule came from an installed example configuration.</param>
/// <param name="IsDebug">Whether the rule came from a debug configuration.</param>
/// <param name="IsTour">Whether the rule belongs to the managed practical-tour configuration.</param>
public sealed record DiagnosticRuleSource(
    DiagnosticRule Rule,
    string Path,
    bool IsExample,
    bool IsDebug,
    bool IsTour);
