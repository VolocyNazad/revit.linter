namespace Revit.Linter.DiagnosticReportPresenter.Exporting;

internal interface IDiagnosticReportExporter
{
    string Extension { get; }
    string Filter { get; }

    void Export(
        string fileName,
        DiagnosticReportExportContext context,
        DiagnosticReportExportDocument document);
}
