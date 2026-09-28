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
        IReadOnlyCollection<DiagnosticReportExportItem> items)
    {
        ISerializer serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        File.WriteAllText(fileName, serializer.Serialize(items), new UTF8Encoding(false));
    }
}
