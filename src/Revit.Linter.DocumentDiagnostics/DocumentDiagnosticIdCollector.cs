using System.Reflection;

namespace Revit.Linter.DocumentDiagnostics;

internal static class DocumentDiagnosticIdCollector
{
    // Every built-in diagnostic carries the BuiltIn tag plus the document-kind tags that mirror its
    // document filter: a check that rejects family documents gets Project, one that only accepts them
    // gets Family, and a check that runs in both gets both tags.
    private static readonly string[] ProjectAndFamily = [DiagnosticTags.Project, DiagnosticTags.Family];

    public static readonly DocumentDiagnosticId StartingViewNotSet = Create("DOC001", DiagnosticSeverity.Message, DiagnosticTags.Project);
    public static readonly DocumentDiagnosticId RevitWarnings = Create("RVT001", DiagnosticSeverity.Warning, DiagnosticTags.Project);
    public static readonly DocumentDiagnosticId UnusedSizeTables = Create("DOC002", DiagnosticSeverity.Message, DiagnosticTags.Family);
    public static readonly DocumentDiagnosticId PreviewNotSet = Create("DOC003", DiagnosticSeverity.Message, ProjectAndFamily);

    private static readonly Lazy<IReadOnlyList<DocumentDiagnosticId>> _allDiagnosticIds =
        new(typeof(DocumentDiagnosticIdCollector)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(DocumentDiagnosticId))
            .Select(field => (DocumentDiagnosticId)field.GetValue(null)!).Where(i => i != null)
            .ToList);

    internal static IReadOnlyList<DocumentDiagnosticId> GetAllDiagnosticIds() => _allDiagnosticIds.Value;

    private static DocumentDiagnosticId Create(string code, DiagnosticSeverity severity, params string[] systemTags) => new(
        code,
        DocumentDiagnosticLocalizations.GetString($"{code}_description"),
        DocumentDiagnosticLocalizations.GetString($"{code}_message"),
        severity,
        true,
        false,
        string.Empty,
        systemTags: [DiagnosticTags.BuiltIn, .. systemTags]);
}
