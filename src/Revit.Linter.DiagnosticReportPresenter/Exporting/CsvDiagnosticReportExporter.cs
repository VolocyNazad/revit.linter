using System.IO;
using System.Text;

namespace Revit.Linter.DiagnosticReportPresenter.Exporting;

internal sealed class CsvDiagnosticReportExporter : IDiagnosticReportExporter
{
    public string Extension => ".csv";
    public string Filter => "CSV (*.csv)|*.csv";

    public void Export(
        string fileName,
        DiagnosticReportExportContext context,
        IReadOnlyCollection<DiagnosticReportExportItem> items)
    {
        string listSeparator = context.Culture.TextInfo.ListSeparator;
        char delimiter = listSeparator.Length > 0 ? listSeparator[0] : ',';
        StringBuilder content = new();
        AppendRow(content, delimiter,
            context.SeverityHeader,
            context.CodeHeader,
            context.MessageHeader,
            context.DocumentHeader,
            context.CreatedHeader);

        foreach (DiagnosticReportExportItem item in items)
            AppendRow(content, delimiter,
                item.Severity,
                item.Code,
                item.Message,
                item.Document,
                item.Created.ToString("G", context.Culture));

        File.WriteAllText(fileName, content.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static void AppendRow(StringBuilder builder, char delimiter, params string?[] values)
    {
        for (int index = 0; index < values.Length; index++)
        {
            if (index > 0) builder.Append(delimiter);

            string value = values[index] ?? string.Empty;
            bool requiresEscaping = value.Contains(delimiter)
                || value.Contains('"')
                || value.Contains('\r')
                || value.Contains('\n');
            if (!requiresEscaping)
            {
                builder.Append(value);
                continue;
            }

            builder.Append('"');
            builder.Append(value.Replace("\"", "\"\""));
            builder.Append('"');
        }

        builder.AppendLine();
    }
}
