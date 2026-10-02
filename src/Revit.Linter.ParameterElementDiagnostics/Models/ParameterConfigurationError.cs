namespace Revit.Linter.ParameterElementDiagnostics.Models;

/// <summary>Identifies why a parameter diagnostic rule was rejected while loading the configuration.</summary>
internal enum ParameterConfigurationErrorKind
{
    /// <summary>A required field is missing or empty; the error value is the field name.</summary>
    MissingValue,
    /// <summary>The parameter GUID is not a valid GUID; the error value is the rejected text.</summary>
    InvalidGuid,
    /// <summary>The parameter group is not valid for the running Revit version.</summary>
    UnknownGroup,
    /// <summary>A category is not a known built-in category.</summary>
    UnknownCategory
}

/// <summary>Describes one reason a parameter diagnostic rule was rejected.</summary>
/// <param name="Kind">The kind of problem.</param>
/// <param name="RuleCode">The code of the rejected rule, or an empty string when the rule has none.</param>
/// <param name="ParameterName">The affected parameter, or <see langword="null"/> for a rule-level problem.</param>
/// <param name="Value">The field name for a missing value; otherwise the rejected text.</param>
internal sealed record ParameterConfigurationError(
    ParameterConfigurationErrorKind Kind, string RuleCode, string? ParameterName, string Value);
