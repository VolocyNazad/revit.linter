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
        IReadOnlyCollection<DiagnosticReportExportItem> items)
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        File.WriteAllText(fileName, JsonSerializer.Serialize(items, options), new UTF8Encoding(false));
    }
}
