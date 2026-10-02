using Revit.Linter.ParameterElementDiagnostics.Models;

namespace Revit.Linter.ParameterElementDiagnostics.Infrastructure.Utils;

/// <summary>Turns configuration errors into the localized text shown to the user.</summary>
internal static class ParameterConfigurationErrorFormatter
{
    /// <summary>Formats the errors of all skipped rules of one configuration file.</summary>
    /// <param name="fileName">The configuration file name shown as the heading.</param>
    /// <param name="errors">The errors of the skipped rules, in file order.</param>
    public static string Format(string fileName, IEnumerable<ParameterConfigurationError> errors)
    {
        List<string> lines = [fileName];
        foreach (IGrouping<string, ParameterConfigurationError> rule in errors.GroupBy(error => error.RuleCode))
        {
            lines.Add(string.Empty);
            lines.Add(rule.Key.Length == 0
                ? ParameterElementDiagnosticLocalizations.GetString("configurationRuleWithoutCodeSkipped_message")
                : ParameterElementDiagnosticLocalizations.GetString("configurationRuleSkipped_message", rule.Key));
            lines.AddRange(rule.Select(error => "- " + FormatError(error)));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatError(ParameterConfigurationError error)
    {
        string text = ParameterElementDiagnosticLocalizations.GetString(
            error.Kind switch
            {
                ParameterConfigurationErrorKind.MissingValue => "configurationMissingValue_message",
                ParameterConfigurationErrorKind.InvalidGuid => "configurationInvalidGuid_message",
                ParameterConfigurationErrorKind.UnknownGroup => "configurationUnknownGroup_message",
                _ => "configurationUnknownCategory_message"
            },
            error.Value);

        return error.ParameterName is null
            ? text
            : ParameterElementDiagnosticLocalizations.GetString(
                "configurationParameterError_message", error.ParameterName, text);
    }
}
