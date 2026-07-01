using Autodesk.Revit.DB;

namespace Revit.Linter.DiagnosticReportProvider.Abstractions.Models;

/// <summary>
/// Отчет диагностики
/// </summary>
/// <param name="Code"> Код диагностики </param>
/// <param name="Severity"> Серьезность </param>
/// <param name="Document"> Документ, в котором выполнялась диагностика </param>
/// <param name="Target"> Объект проверки </param>
/// <param name="Message"> Сообщение о результатах диагностики </param>
/// <param name="IsObsolete"> Указывает, это отчет об устаревшей проверке или нет </param>
/// <param name="ObsoleteDescription"> Описание причины устаревания </param>
public sealed record DiagnosticReport(
    string Code, DiagnosticSeverity Severity, Document Document, object? Target, DiagnosticReportMessage Message, bool IsObsolete, string ObsoleteDescription = "")
{
    public DateTime Created { get; } = DateTime.Now;
}
