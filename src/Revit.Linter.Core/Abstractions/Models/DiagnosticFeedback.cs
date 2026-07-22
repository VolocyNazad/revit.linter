namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>
/// Результат диагностики
/// </summary>
/// <param name="Verdict"> Вердикт </param>
/// <param name="EnrichMessageArgs"></param>
/// <param name="EnrichTargetDependencies"></param>
public record DiagnosticFeedback(DiagnosticVerdict Verdict, Dictionary<string, object>? EnrichMessageArgs = null, params object[] EnrichTargetDependencies)
{
    public static readonly DiagnosticFeedback Valid = new(DiagnosticVerdict.Valid);
}
