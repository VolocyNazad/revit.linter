using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.ConfigurationPath.Abstractions.Models;

/// <summary>Describes the effective state of one diagnostic to persist into its configuration file.</summary>
/// <param name="Code">The stable diagnostic code as written in the file.</param>
/// <param name="RuleType">The rule type of the diagnostic.</param>
/// <param name="IsActive">Whether the diagnostic participates in execution.</param>
/// <param name="Severity">The severity of the findings the diagnostic reports.</param>
/// <param name="IsHidden">Whether the diagnostic is hidden from the interface, execution and reports.</param>
public readonly record struct DiagnosticRuleStateUpdate(
    string Code,
    DiagnosticRuleType RuleType,
    bool IsActive,
    DiagnosticSeverity Severity,
    bool IsHidden);
