using System.IO;
using Revit.Linter.DiagnosticReportExporting.Abstractions.Models;
using Revit.Linter.DiagnosticReportExporting.Abstractions.Services;

namespace Revit.Linter.DiagnosticReportExporting.Infrastructure.Services;

internal sealed class DiagnosticReportExportService : IDiagnosticReportExportService
{
    private const string SchemaVersion = "1.0";
    private const string AllDocumentsScope = "AllDocuments";
    private const string DocumentScope = "Document";

    private readonly IReadOnlyList<IDiagnosticReportExporter> _exporters;
    private readonly ISaveFileDialogService _saveFileDialogService;

    public DiagnosticReportExportService(
        IEnumerable<IDiagnosticReportExporter> exporters,
        ISaveFileDialogService saveFileDialogService)
    {
        _exporters = exporters.ToArray();
        _saveFileDialogService = saveFileDialogService;
    }

    public DiagnosticReportExportResult Export(DiagnosticReportExportRequest request)
    {
        IReadOnlyList<IDiagnosticReportExporter> exporters = _exporters;
        if (exporters.Count == 0)
            return new DiagnosticReportExportResult(false, null, null);

        string documentTitle = string.IsNullOrWhiteSpace(request.TargetDocumentTitle)
            ? request.Labels.AllDocumentsText
            : request.TargetDocumentTitle ?? request.Labels.AllDocumentsText;

        SaveFileDialogResult dialog = _saveFileDialogService.ShowSaveDialog(new SaveFileDialogOptions(
            CreateExportFileName(documentTitle, request.ExportedAt.LocalDateTime),
            exporters[0].Extension,
            exporters.Select(exporter => exporter.Filter).ToArray()));

        if (!dialog.Confirmed)
            return new DiagnosticReportExportResult(false, null, null);

        int exporterIndex = dialog.FilterIndex - 1;
        IDiagnosticReportExporter exporter = exporterIndex >= 0 && exporterIndex < exporters.Count
            ? exporters[exporterIndex]
            : exporters[0];
        string fileName = Path.ChangeExtension(dialog.FileName, exporter.Extension);
        DiagnosticReportExportDocument document = new(
            SchemaVersion,
            new DiagnosticReportExportMetadata(
                request.ExportedAt,
                request.RevitVersion,
                request.RevitBuild,
                request.AddInVersion,
                documentTitle,
                string.IsNullOrWhiteSpace(request.TargetDocumentTitle) ? AllDocumentsScope : DocumentScope,
                new DiagnosticReportExportFilters(
                    string.IsNullOrWhiteSpace(request.SearchText) ? null : request.SearchText,
                    request.Severities,
                    request.States),
                request.TotalItemCount,
                request.Items.Count),
            request.Items);
        DiagnosticReportExportContext context = new()
        {
            Culture = request.Labels.Culture,
            UiCulture = request.Labels.UiCulture,
            SeverityHeader = request.Labels.SeverityHeader,
            CodeHeader = request.Labels.CodeHeader,
            MessageHeader = request.Labels.MessageHeader,
            DocumentHeader = request.Labels.DocumentHeader,
            CreatedHeader = request.Labels.CreatedHeader,
            ReportTitle = request.Labels.ReportTitle,
            GeneratedLabel = request.Labels.GeneratedLabel,
            TotalLabel = request.Labels.TotalLabel,
            SummaryByCodeTitle = request.Labels.SummaryByCodeTitle,
            CountHeader = request.Labels.CountHeader,
            DetailsTitle = request.Labels.DetailsTitle,
            NoResultsText = request.Labels.NoResultsText,
            ErrorText = request.Labels.ErrorText,
            WarningText = request.Labels.WarningText,
            MessageText = request.Labels.MessageText
        };
        exporter.Export(fileName, context, document);
        return new DiagnosticReportExportResult(true, fileName, exporter.Extension);
    }

    private static string CreateExportFileName(string document, DateTime timestamp)
    {
        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        string safeDocument = string.Concat(document.Select(character =>
            invalidCharacters.Contains(character) ? '_' : character));
        return $"{safeDocument}_{timestamp:yyyy-MM-dd_HH-mm-ss}";
    }
}
