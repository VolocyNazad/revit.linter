using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.DiagnosticConfiguration.Validation;

/// <summary>Validates one configuration entry against the schema of its declared rule type.</summary>
/// <remarks>
/// The validator is Revit-independent, so the same rules protect the add-in loader and the standalone
/// diagnostics editor. It reports the problems a rule has before it can be registered; it does not
/// validate formulas, pipeline step semantics or Revit-specific identifiers, which require the
/// runtime that owns the corresponding module. <see cref="Validate"/> returns the English text and
/// <see cref="ValidateIssues"/> the same problems as stable codes, so a host can localize them.
/// </remarks>
public static class DiagnosticRuleValidator
{
    /// <summary>
    /// Validates the entry and reports every problem that prevents it from being registered.
    /// </summary>
    /// <param name="rule">The configuration entry.</param>
    /// <returns>The user-facing problem descriptions; an empty list when the rule is valid.</returns>
    public static IReadOnlyList<string> Validate(DiagnosticRule rule) =>
        ValidateIssues(rule).Select(Describe).ToList();

    /// <summary>Validates the entry and reports every problem as a stable code and its arguments.</summary>
    /// <param name="rule">The configuration entry.</param>
    /// <returns>The schema problems; an empty list when the rule is valid.</returns>
    public static IReadOnlyList<DiagnosticRuleIssue> ValidateIssues(DiagnosticRule rule)
    {
        List<DiagnosticRuleIssue> issues = [];
        if (!DiagnosticRuleTypes.TryParse(rule.Type, out DiagnosticRuleType type))
        {
            issues.Add(new DiagnosticRuleIssue(
                DiagnosticRuleIssueCode.UnknownType, [rule.Type ?? "(missing)"]));
            return issues;
        }

        if (string.IsNullOrWhiteSpace(rule.Code))
            issues.Add(new DiagnosticRuleIssue(DiagnosticRuleIssueCode.MissingCode, []));

        switch (type)
        {
            case DiagnosticRuleType.Custom:
                Require(rule.TakeDocument, "takeDocument", issues);
                Require(rule.Take, "take", issues);
                Require(rule.Check, "check", issues);
                Reject(rule, issues, "andTake", rule.AndTake,
                    "parameters", rule.Parameters is { Length: > 0 });
                break;

            case DiagnosticRuleType.Collision:
                Require(rule.TakeDocument, "takeDocument", issues);
                Require(rule.Take, "take", issues);
                Require(rule.AndTake, "andTake", issues);
                Require(rule.GroupBy, "groupBy", issues);
                Reject(rule, issues, "check", rule.Check,
                    "parameters", rule.Parameters is { Length: > 0 });
                break;

            case DiagnosticRuleType.Parameter:
                Require(rule.Take, "take", issues);
                if (rule.Parameters is not { Length: > 0 })
                    issues.Add(new DiagnosticRuleIssue(DiagnosticRuleIssueCode.MissingParameters, []));
                Reject(rule, issues, "check", rule.Check,
                    "andTake", rule.AndTake,
                    "groupBy", rule.GroupBy);
                break;

            case DiagnosticRuleType.BuiltIn:
                Reject(rule, issues,
                    "description", !string.IsNullOrWhiteSpace(rule.Description),
                    "message", rule.Message,
                    "takeDocument", rule.TakeDocument,
                    "take", rule.Take,
                    "check", rule.Check,
                    "andTake", rule.AndTake,
                    "groupBy", rule.GroupBy,
                    "parameters", rule.Parameters is { Length: > 0 },
                    "tags", rule.Tags is { Length: > 0 },
                    "fixes", rule.Fixes is { Length: > 0 },
                    "visualizations", rule.Visualizations is { Length: > 0 },
                    "isObsolete", rule.IsObsolete,
                    "obsoleteDescription", rule.ObsoleteDescription);
                break;

            default:
                issues.Add(new DiagnosticRuleIssue(
                    DiagnosticRuleIssueCode.UnsupportedType, [type.ToString()]));
                break;
        }

        return issues;
    }

    /// <summary>Formats one issue as the English description used by logs and legacy callers.</summary>
    /// <param name="issue">The issue to format.</param>
    /// <returns>The user-facing English description.</returns>
    public static string Describe(DiagnosticRuleIssue issue) => issue.Code switch
    {
        DiagnosticRuleIssueCode.UnknownType =>
            $"unknown type '{issue.Arguments[0]}'; expected custom, collision, parameter or builtIn",
        DiagnosticRuleIssueCode.MissingCode => "the code is missing",
        DiagnosticRuleIssueCode.MissingField => $"the {issue.Arguments[0]} field is missing",
        DiagnosticRuleIssueCode.MissingParameters => "the parameters list is missing or empty",
        DiagnosticRuleIssueCode.UnsupportedField =>
            $"the {issue.Arguments[0]} field is not supported by type '{issue.Arguments[1]}'",
        DiagnosticRuleIssueCode.UnsupportedType => $"unsupported type '{issue.Arguments[0]}'",
        _ => "the rule is not valid",
    };

    private static void Require(string? value, string name, List<DiagnosticRuleIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
            issues.Add(new DiagnosticRuleIssue(DiagnosticRuleIssueCode.MissingField, [name]));
    }

    private static void Reject(DiagnosticRule rule, List<DiagnosticRuleIssue> issues, params object?[] fields)
    {
        for (int index = 0; index + 1 < fields.Length; index += 2)
        {
            string name = (string)fields[index]!;
            object? value = fields[index + 1];
            bool isPresent = value switch
            {
                null => false,
                bool flag => flag,
                string text => !string.IsNullOrWhiteSpace(text),
                _ => true,
            };
            if (isPresent)
                issues.Add(new DiagnosticRuleIssue(
                    DiagnosticRuleIssueCode.UnsupportedField, [name, rule.Type ?? string.Empty]));
        }
    }
}
