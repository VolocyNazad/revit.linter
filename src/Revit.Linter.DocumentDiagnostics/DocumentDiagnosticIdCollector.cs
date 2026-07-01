using System.Reflection;

namespace Revit.Linter.DocumentDiagnostics;

internal static class DocumentDiagnosticIdCollector
{
    public readonly static DocumentDiagnosticId StartingViewNotSet = new(
        "DOC001",
        "Проверка 'Задан ли начальный вид документу'.",
        "Документу с наименованием '{documentTitle}' не задано начальное окно. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Message,
        true,
        false, 
        string.Empty);


    private static readonly Lazy<IReadOnlyList<DocumentDiagnosticId>> _allDiagnosticIds =
        new(typeof(DocumentDiagnosticIdCollector)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(DocumentDiagnosticId))
            .Select(field => (DocumentDiagnosticId)field.GetValue(null)!).Where(i => i != null)
            .ToList);
    internal static IReadOnlyList<DocumentDiagnosticId> GetAllDiagnosticIds() => _allDiagnosticIds.Value;
}
