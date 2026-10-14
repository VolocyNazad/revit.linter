using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Revit.Linter.DiagnosticReportExporting.Abstractions.Models;
using Revit.Linter.DiagnosticReportExporting.Abstractions.Services;

namespace Revit.Linter.DiagnosticReportExporting.Infrastructure.Services;

internal sealed class YamlDiagnosticReportExporter : IDiagnosticReportExporter
{
    public string Extension => ".yaml";
    public string Filter => "YAML (*.yaml)|*.yaml";

    public void Export(
        string fileName,
        DiagnosticReportExportContext context,
        DiagnosticReportExportDocument document)
    {
        ISerializer serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(new DateTimeOffsetYamlTypeConverter())
            .Build();
        File.WriteAllText(fileName, serializer.Serialize(document), new UTF8Encoding(false));
    }
}
