namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Describes a diagnostic verdict and the data needed to present its finding.</summary>
/// <param name="Verdict">Whether the checked target satisfies the diagnostic.</param>
/// <param name="AdditionalMessageArguments">Values available to the diagnostic message template.</param>
/// <param name="AdditionalTargetDependencies">Related objects that help explain or visualize the finding.</param>
public record DiagnosticFeedback(DiagnosticVerdict Verdict, Dictionary<string, object>? AdditionalMessageArguments = null, params object[] AdditionalTargetDependencies)
{
    /// <summary>
    /// Gets the message format that replaces the diagnostic message for this finding.
    /// </summary>
    /// <remarks>
    /// A diagnostic uses it when one finding needs different wording, for example an uncertain result
    /// that must not be presented in the same words as a definite one. The substituted values are still
    /// read from <see cref="AdditionalMessageArguments"/> and the standard arguments, so the format
    /// uses the same placeholders as the diagnostic message.
    /// </remarks>
    public string? MessageFormat { get; init; }

    /// <summary>Represents a successful diagnostic result without additional message data.</summary>
    public static readonly DiagnosticFeedback Valid = new(DiagnosticVerdict.Valid);
}
