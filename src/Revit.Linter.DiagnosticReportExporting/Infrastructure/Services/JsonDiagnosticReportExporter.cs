using System.IO;
using System.Text;
using System.Text.Json;
using Revit.Linter.DiagnosticReportExporting.Abstractions.Models;
using Revit.Linter.DiagnosticReportExporting.Abstractions.Services;

namespace Revit.Linter.DiagnosticReportExporting.Infrastructure.Services;

internal sealed class JsonDiagnosticReportExporter : IDiagnosticReportExporter
{
    public string Extension => ".json";
    public string Filter => "JSON (*.json)|*.json";

    public void Export(
        string fileName,
        DiagnosticReportExportContext context,
        DiagnosticReportExportDocument document)
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        File.WriteAllText(fileName, JsonSerializer.Serialize(document, options), new UTF8Encoding(false));
    }
}
