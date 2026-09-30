using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Revit.Linter.DiagnosticReportPresenter.Exporting;

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
            .Build();
        File.WriteAllText(fileName, serializer.Serialize(document), new UTF8Encoding(false));
    }
}
