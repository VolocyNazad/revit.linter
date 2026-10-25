namespace Revit.Linter.DiagnosticConfiguration.Validation;

/// <summary>Identifies one schema problem of a configuration rule.</summary>
public enum DiagnosticRuleIssueCode
{
    /// <summary>The type discriminator is unknown or missing.</summary>
    UnknownType,

    /// <summary>The rule code is missing.</summary>
    MissingCode,

    /// <summary>A required field is missing.</summary>
    MissingField,

    /// <summary>The parameters list of a parameter rule is missing or empty.</summary>
    MissingParameters,

    /// <summary>A field is not supported by the declared type.</summary>
    UnsupportedField,

    /// <summary>The parsed type is not supported.</summary>
    UnsupportedType,
}

/// <summary>Describes one schema problem of a configuration rule without formatting it for the user.</summary>
/// <param name="Code">The stable problem code.</param>
/// <param name="Arguments">The values referenced by the problem, already converted to text.</param>
/// <remarks>
/// The codes let the add-in and the standalone editor describe the same problem in the user's language:
/// the shared validator owns the rule, and each host owns its resources. <see cref="DiagnosticRuleValidator.Validate"/>
/// keeps returning the English text for callers that only log the message.
/// </remarks>
public sealed record DiagnosticRuleIssue(
    DiagnosticRuleIssueCode Code,
    IReadOnlyList<string> Arguments);
