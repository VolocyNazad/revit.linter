using System.IO;
using System.Net;
using System.Text;

namespace Revit.Linter.DiagnosticReportPresenter.Exporting;

internal sealed class HtmlDiagnosticReportExporter : IDiagnosticReportExporter
{
    public string Extension => ".html";
    public string Filter => "HTML (*.html)|*.html";

    public void Export(
        string fileName,
        DiagnosticReportExportContext context,
        IReadOnlyCollection<DiagnosticReportExportItem> items)
    {
        int errorCount = items.Count(item => item.Severity == context.ErrorText);
        int warningCount = items.Count(item => item.Severity == context.WarningText);
        int messageCount = items.Count(item => item.Severity == context.MessageText);

        StringBuilder content = new();
        content.AppendLine("<!DOCTYPE html>")
            .Append("<html lang=\"").Append(Encode(context.UiCulture.TwoLetterISOLanguageName)).AppendLine("\">")
            .AppendLine("<head>")
            .AppendLine("<meta charset=\"utf-8\">")
            .AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">")
            .Append("<title>").Append(Encode(context.ReportTitle)).AppendLine("</title>")
            .AppendLine("<style>")
            .AppendLine("body{margin:0;background:#f5f7fa;color:#172033;font-family:Segoe UI,Arial,sans-serif;font-size:14px}")
            .AppendLine("main{max-width:1200px;margin:0 auto;padding:32px 24px 48px}")
            .AppendLine("h1{margin:0 0 8px;font-size:28px}h2{margin:32px 0 12px;font-size:18px}")
            .AppendLine(".meta{color:#5d6678;margin-bottom:24px}.meta span+span:before{content:' · ';padding:0 6px}")
            .AppendLine(".cards{display:grid;grid-template-columns:repeat(4,minmax(130px,1fr));gap:12px}")
            .AppendLine(".card{background:#fff;border:1px solid #dfe3eb;border-radius:8px;padding:16px}.card strong{display:block;font-size:26px;margin-bottom:4px}")
            .AppendLine(".error{border-top:4px solid #c62828}.warning{border-top:4px solid #ef8c00}.message{border-top:4px solid #1976d2}.total{border-top:4px solid #48566a}")
            .AppendLine(".table-wrap{overflow-x:auto;background:#fff;border:1px solid #dfe3eb;border-radius:8px}")
            .AppendLine("table{width:100%;border-collapse:collapse}th,td{padding:10px 12px;text-align:left;vertical-align:top;border-bottom:1px solid #e6e9ef}th{background:#eef1f6;white-space:nowrap}tr:last-child td{border-bottom:0}.count{width:1%;text-align:right}.message-cell{white-space:pre-wrap;min-width:320px}")
            .AppendLine(".empty{padding:24px;text-align:center;color:#5d6678}")
            .AppendLine("@media(max-width:700px){main{padding:20px 12px}.cards{grid-template-columns:repeat(2,1fr)}}")
            .AppendLine("@media print{body{background:#fff}main{max-width:none;padding:0}.card,.table-wrap{break-inside:avoid}.table-wrap{overflow:visible}}")
            .AppendLine("</style>")
            .AppendLine("</head>")
            .AppendLine("<body><main>")
            .Append("<h1>").Append(Encode(context.ReportTitle)).AppendLine("</h1>")
            .Append("<div class=\"meta\"><span>").Append(Encode(context.DocumentHeader)).Append(": ")
            .Append(Encode(context.DocumentTitle)).Append("</span><span>").Append(Encode(context.GeneratedLabel)).Append(": ")
            .Append(Encode(context.ExportedAt.ToString("G", context.Culture))).AppendLine("</span></div>")
            .AppendLine("<section class=\"cards\">");

        AppendSummaryCard(content, context, "total", context.TotalLabel, items.Count);
        AppendSummaryCard(content, context, "error", context.ErrorText, errorCount);
        AppendSummaryCard(content, context, "warning", context.WarningText, warningCount);
        AppendSummaryCard(content, context, "message", context.MessageText, messageCount);

        content.AppendLine("</section>")
            .Append("<h2>").Append(Encode(context.SummaryByCodeTitle)).AppendLine("</h2>")
            .AppendLine("<div class=\"table-wrap\"><table><thead><tr>")
            .Append("<th>").Append(Encode(context.CodeHeader)).Append("</th><th class=\"count\">")
            .Append(Encode(context.CountHeader)).AppendLine("</th></tr></thead><tbody>");

        foreach (IGrouping<string, DiagnosticReportExportItem> group in items
                     .GroupBy(item => item.Code)
                     .OrderByDescending(group => group.Count())
                     .ThenBy(group => group.Key, StringComparer.CurrentCulture))
        {
            content.Append("<tr><td>").Append(Encode(group.Key)).Append("</td><td class=\"count\">")
                .Append(group.Count().ToString(context.Culture)).AppendLine("</td></tr>");
        }

        content.AppendLine("</tbody></table></div>")
            .Append("<h2>").Append(Encode(context.DetailsTitle)).AppendLine("</h2>");

        if (items.Count == 0)
        {
            content.Append("<div class=\"table-wrap empty\">").Append(Encode(context.NoResultsText)).AppendLine("</div>");
        }
        else
        {
            AppendDetailsTable(content, context, items);
        }

        content.AppendLine("</main></body></html>");
        File.WriteAllText(fileName, content.ToString(), new UTF8Encoding(false));
    }

    private static void AppendSummaryCard(
        StringBuilder content,
        DiagnosticReportExportContext context,
        string style,
        string title,
        int count)
    {
        content.Append("<div class=\"card ").Append(style).Append("\"><strong>")
            .Append(count.ToString(context.Culture)).Append("</strong><span>")
            .Append(Encode(title)).AppendLine("</span></div>");
    }

    private static void AppendDetailsTable(
        StringBuilder content,
        DiagnosticReportExportContext context,
        IEnumerable<DiagnosticReportExportItem> items)
    {
        content.AppendLine("<div class=\"table-wrap\"><table><thead><tr>")
            .Append("<th>").Append(Encode(context.SeverityHeader)).Append("</th><th>").Append(Encode(context.CodeHeader))
            .Append("</th><th>").Append(Encode(context.MessageHeader)).Append("</th><th>").Append(Encode(context.DocumentHeader))
            .Append("</th><th>").Append(Encode(context.CreatedHeader)).AppendLine("</th></tr></thead><tbody>");

        foreach (DiagnosticReportExportItem item in items)
        {
            content.Append("<tr><td>").Append(Encode(item.Severity)).Append("</td><td>")
                .Append(Encode(item.Code)).Append("</td><td class=\"message-cell\">")
                .Append(Encode(item.Message)).Append("</td><td>").Append(Encode(item.Document))
                .Append("</td><td>").Append(Encode(item.Created.ToString("G", context.Culture)))
                .AppendLine("</td></tr>");
        }

        content.AppendLine("</tbody></table></div>");
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
