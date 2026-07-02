namespace Revit.Linter.FixReportProvider.Abstractions.Models;

/// <summary>
/// Отчет исправления
/// </summary>
/// <param name="Code"> Код диагностики </param>
/// <param name="DocumentTitle"> Заголовок документа </param>
/// <param name="Message"> Сообщение о результатах исправлений </param>
public sealed record FixReport(string Code, string DocumentTitle, FixReportMessage Message)
{
    public DateTime Created {  get; } = DateTime.Now;
}
