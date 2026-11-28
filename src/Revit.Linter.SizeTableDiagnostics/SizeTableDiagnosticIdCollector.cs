namespace Revit.Linter.SizeTableDiagnostics;

internal static class SizeTableDiagnosticIdCollector
{
    public static readonly DocumentDiagnosticId UnusedSizeTables =
        Create("DOC002", DiagnosticSeverity.Warning);
    public static readonly DocumentDiagnosticId MissingSizeTable =
        Create("DOC005", DiagnosticSeverity.Warning);
    public static readonly DocumentDiagnosticId MissingSizeTableColumn =
        Create("DOC006", DiagnosticSeverity.Warning);
    public static readonly DocumentDiagnosticId UnresolvedSizeTableArgument =
        Create("DOC007", DiagnosticSeverity.Message);

    private static DocumentDiagnosticId Create(string code, DiagnosticSeverity severity) => new(
        code,
        SizeTableDiagnosticLocalizations.GetString($"{code}_description"),
        SizeTableDiagnosticLocalizations.GetString($"{code}_message"),
        severity,
        true,
        false,
        string.Empty,
        systemTags: [DiagnosticTags.BuiltIn, DiagnosticTags.Family]);
}
