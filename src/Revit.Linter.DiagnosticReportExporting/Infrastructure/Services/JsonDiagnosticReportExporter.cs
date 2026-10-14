using System.IO;
using System.Text;
using System.Text.Json;
using Revit.Linter.DiagnosticReportExporting.Abstractions.Models;
using Revit.Linter.DiagnosticReportExporting.Abstractions.Services;

namespace Revit.Linter.DiagnosticReportExporting.Infrastructure.Services;

internal sealed class JsonDiagnosticReportExporter : IDiagnosticReportExporter
{
    // JsonSerializerOptions is intended for reuse: it caches serialization metadata.
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public string Extension => ".json";
    public string Filter => "JSON (*.json)|*.json";

    public void Export(
        string fileName,
        DiagnosticReportExportContext context,
        DiagnosticReportExportDocument document)
    {
        File.WriteAllText(fileName, JsonSerializer.Serialize(document, Options), new UTF8Encoding(false));
    }
}
