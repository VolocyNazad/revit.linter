namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>
/// Diagnostic result
/// </summary>
/// <param name="Verdict"> Verdict </param>
/// <param name="AdditionalMessageArguments"></param>
/// <param name="AdditionalTargetDependencies"></param>
public record DiagnosticFeedback(DiagnosticVerdict Verdict, Dictionary<string, object>? AdditionalMessageArguments = null, params object[] AdditionalTargetDependencies)
{
    public static readonly DiagnosticFeedback Valid = new(DiagnosticVerdict.Valid);
}
