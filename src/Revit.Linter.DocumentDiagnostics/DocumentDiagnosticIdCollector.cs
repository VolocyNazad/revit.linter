using System.Reflection;

namespace Revit.Linter.DocumentDiagnostics;

internal static class DocumentDiagnosticIdCollector
{
    public static readonly DocumentDiagnosticId StartingViewNotSet = Create("DOC001", DiagnosticSeverity.Message);
    public static readonly DocumentDiagnosticId RevitWarnings = Create("RVT", DiagnosticSeverity.Warning);

    private static readonly Lazy<IReadOnlyList<DocumentDiagnosticId>> _allDiagnosticIds =
        new(typeof(DocumentDiagnosticIdCollector)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(DocumentDiagnosticId))
            .Select(field => (DocumentDiagnosticId)field.GetValue(null)!).Where(i => i != null)
            .ToList);

    internal static IReadOnlyList<DocumentDiagnosticId> GetAllDiagnosticIds() => _allDiagnosticIds.Value;

    private static DocumentDiagnosticId Create(string code, DiagnosticSeverity severity) => new(
        code,
        DocumentDiagnosticLocalizations.GetString($"{code}_description"),
        DocumentDiagnosticLocalizations.GetString($"{code}_message"),
        severity,
        true,
        false,
        string.Empty);
}
