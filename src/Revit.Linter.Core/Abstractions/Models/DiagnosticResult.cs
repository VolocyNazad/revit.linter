namespace Revit.Linter.Core.Abstractions.Models;

public record DiagnosticResult(DiagnosticVerdict Verdict, Dictionary<string, object>? MessageArgs = null)
{
    public static readonly DiagnosticResult Valid = new(DiagnosticVerdict.Valid);
}
