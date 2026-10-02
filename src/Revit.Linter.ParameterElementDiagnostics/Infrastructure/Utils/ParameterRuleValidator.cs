using Revit.Linter.ParameterElementDiagnostics.Models;

namespace Revit.Linter.ParameterElementDiagnostics.Infrastructure.Utils;

/// <summary>Validates a deserialized parameter diagnostic rule before it is registered.</summary>
/// <remarks>
/// YAML deserialization does not enforce required members, and identifier names depend on the Revit version.
/// Checking a rule here turns what would be an exception during a diagnostic run into a described
/// configuration error. Revit-specific knowledge is supplied through the predicates, which keeps this type
/// independent of the Revit API.
/// </remarks>
internal static class ParameterRuleValidator
{
    /// <summary>Collects every problem that prevents <paramref name="rule"/> from being executed.</summary>
    /// <param name="rule">The deserialized rule.</param>
    /// <param name="isKnownCategory">Returns whether a category identifier is valid for the running Revit.</param>
    /// <param name="isKnownGroup">Returns whether a group identifier is valid for the running Revit.</param>
    /// <returns>The problems found; empty when the rule can be registered.</returns>
    public static IReadOnlyList<ParameterConfigurationError> Validate(
        DiagnosticRule rule, Func<string, bool> isKnownCategory, Func<string, bool> isKnownGroup)
    {
        List<ParameterConfigurationError> errors = [];
        string ruleCode = rule.Code ?? string.Empty;

        if (IsBlank(rule.Code))
            errors.Add(Missing(ruleCode, null, "code"));
        if (IsBlank(rule.Take))
            errors.Add(Missing(ruleCode, null, "take"));
        if (rule.Parameters is not { Count: > 0 })
        {
            errors.Add(Missing(ruleCode, null, "parameters"));
            return errors;
        }

        foreach (ParameterElementData? parameter in rule.Parameters)
        {
            if (parameter is null)
            {
                errors.Add(Missing(ruleCode, null, "parameters"));
                continue;
            }

            ValidateParameter(ruleCode, parameter, isKnownCategory, isKnownGroup, errors);
        }

        return errors;
    }

    private static void ValidateParameter(
        string ruleCode,
        ParameterElementData parameter,
        Func<string, bool> isKnownCategory,
        Func<string, bool> isKnownGroup,
        List<ParameterConfigurationError> errors)
    {
        string? name = IsBlank(parameter.Name) ? null : parameter.Name;
        if (name is null)
            errors.Add(Missing(ruleCode, null, "name"));

        // The GUID is optional: a rule without it looks the parameter up by name.
        if (!IsBlank(parameter.Guid) && !Guid.TryParse(parameter.Guid, out _))
            errors.Add(new(ParameterConfigurationErrorKind.InvalidGuid, ruleCode, name, parameter.Guid));

        // An empty group is passed to the predicate because its validity depends on the Revit version.
        string group = parameter.Group ?? string.Empty;
        if (!isKnownGroup(group))
            errors.Add(new(ParameterConfigurationErrorKind.UnknownGroup, ruleCode, name, group));

        if (parameter.Categories is not { Count: > 0 })
        {
            errors.Add(Missing(ruleCode, name, "categories"));
            return;
        }

        foreach (string? category in parameter.Categories)
        {
            if (IsBlank(category) || !isKnownCategory(category!))
                errors.Add(new(
                    ParameterConfigurationErrorKind.UnknownCategory, ruleCode, name, category ?? string.Empty));
        }
    }

    private static ParameterConfigurationError Missing(string ruleCode, string? parameterName, string field) =>
        new(ParameterConfigurationErrorKind.MissingValue, ruleCode, parameterName, field);

    private static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);
}
