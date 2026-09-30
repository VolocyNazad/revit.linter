using System.IO;
using System.Text;
using System.Text.Json;

namespace Revit.Linter.DiagnosticReportPresenter.Exporting;

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
