using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Revit.Linter.DiagnosticReportExporting.Abstractions.Models;
using Revit.Linter.DiagnosticReportExporting.Abstractions.Services;

namespace Revit.Linter.DiagnosticReportExporting.Infrastructure.Services;

internal sealed class YamlDiagnosticReportExporter : IDiagnosticReportExporter
{
    // SerializerBuilder builds the type graph via reflection: build once and reuse.
    // The built serializer carries only the stateless DateTimeOffset converter.
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithTypeConverter(new DateTimeOffsetYamlTypeConverter())
        .Build();

    public string Extension => ".yaml";
    public string Filter => "YAML (*.yaml)|*.yaml";

    public void Export(
        string fileName,
        DiagnosticReportExportContext context,
        DiagnosticReportExportDocument document)
    {
        File.WriteAllText(fileName, Serializer.Serialize(document), new UTF8Encoding(false));
    }
}
