using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.ConfigurationPath.Services;

/// <summary>Validates one configuration entry against the schema of its declared rule type.</summary>
internal static class DiagnosticRuleValidator
{
    /// <summary>
    /// Validates the entry and reports every problem that prevents it from being registered.
    /// </summary>
    /// <param name="rule">The configuration entry.</param>
    /// <returns>The user-facing problem descriptions; an empty list when the rule is valid.</returns>
    public static IReadOnlyList<string> Validate(DiagnosticRule rule)
    {
        List<string> errors = [];
        if (!DiagnosticRuleTypes.TryParse(rule.Type, out DiagnosticRuleType type))
        {
            errors.Add($"unknown type '{rule.Type ?? "(missing)"}'; expected custom, collision, parameter or builtIn");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(rule.Code))
            errors.Add("the code is missing");

        switch (type)
        {
            case DiagnosticRuleType.Custom:
                Require(rule.TakeDocument, "takeDocument", errors);
                Require(rule.Take, "take", errors);
                Require(rule.Check, "check", errors);
                Reject(rule, errors, "andTake", rule.AndTake,
                    "parameters", rule.Parameters is { Length: > 0 });
                break;

            case DiagnosticRuleType.Collision:
                Require(rule.TakeDocument, "takeDocument", errors);
                Require(rule.Take, "take", errors);
                Require(rule.AndTake, "andTake", errors);
                Require(rule.GroupBy, "groupBy", errors);
                Reject(rule, errors, "check", rule.Check,
                    "parameters", rule.Parameters is { Length: > 0 });
                break;

            case DiagnosticRuleType.Parameter:
                Require(rule.Take, "take", errors);
                if (rule.Parameters is not { Length: > 0 })
                    errors.Add("the parameters list is missing or empty");
                Reject(rule, errors, "check", rule.Check,
                    "andTake", rule.AndTake,
                    "groupBy", rule.GroupBy);
                break;

            case DiagnosticRuleType.BuiltIn:
                Reject(rule, errors,
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
                errors.Add($"unsupported type '{type}'");
                break;
        }

        return errors;
    }

    private static void Require(string? value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"the {name} field is missing");
    }

    private static void Reject(DiagnosticRule rule, List<string> errors, params object?[] fields)
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
                errors.Add($"the {name} field is not supported by type '{rule.Type}'");
        }
    }
}
