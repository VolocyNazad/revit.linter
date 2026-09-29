namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Describes a diagnostic verdict and the data needed to present its finding.</summary>
/// <param name="Verdict">Whether the checked target satisfies the diagnostic.</param>
/// <param name="AdditionalMessageArguments">Values available to the diagnostic message template.</param>
/// <param name="AdditionalTargetDependencies">Related objects that help explain or visualize the finding.</param>
public record DiagnosticFeedback(DiagnosticVerdict Verdict, Dictionary<string, object>? AdditionalMessageArguments = null, params object[] AdditionalTargetDependencies)
{
    /// <summary>Represents a successful diagnostic result without additional message data.</summary>
    public static readonly DiagnosticFeedback Valid = new(DiagnosticVerdict.Valid);
}
