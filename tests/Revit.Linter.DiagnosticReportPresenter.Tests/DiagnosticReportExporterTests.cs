using System.Globalization;
using Revit.Linter.DiagnosticReportPresenter.Exporting;
using Revit.Linter.Testing;

namespace Revit.Linter.DiagnosticReportPresenter.Tests;

/// <summary>
/// Pins the exported report formats with snapshots. JSON and YAML are a versioned contract for external
/// tools, so an unintended change must show up as a snapshot difference.
/// </summary>
public sealed class DiagnosticReportExporterTests : IDisposable
{
    private const string SnapshotDirectory = "tests/Revit.Linter.DiagnosticReportPresenter.Tests/Snapshots";

    private readonly string _outputDirectory = Path.Combine(
        Path.GetTempPath(), nameof(DiagnosticReportExporterTests), Guid.NewGuid().ToString("N"));

    public DiagnosticReportExporterTests() => Directory.CreateDirectory(_outputDirectory);

    public void Dispose() => Directory.Delete(_outputDirectory, recursive: true);

    [Fact]
    public void Csv_export_escapes_delimiters_quotes_and_line_breaks()
    {
        string path = Export(new CsvDiagnosticReportExporter(), CreateContext(listSeparator: ","), CreateDocument());

        Verify("csv", File.ReadAllText(path));
    }

    [Fact]
    public void Csv_export_uses_the_culture_list_separator()
    {
        string path = Export(new CsvDiagnosticReportExporter(), CreateContext(listSeparator: ";"), CreateDocument());

        Verify("csv", File.ReadAllText(path));
    }

    [Fact]
    public void Csv_export_starts_with_a_byte_order_mark()
    {
        string path = Export(new CsvDiagnosticReportExporter(), CreateContext(listSeparator: ","), CreateDocument());

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, File.ReadAllBytes(path).Take(3));
    }

    [Fact]
    public void Json_export_writes_the_versioned_document()
    {
        string path = Export(new JsonDiagnosticReportExporter(), CreateContext(), CreateDocument());

        Verify("json", File.ReadAllText(path));
    }

    [Fact]
    public void Yaml_export_writes_the_versioned_document()
    {
        string path = Export(new YamlDiagnosticReportExporter(), CreateContext(), CreateDocument());

        Verify("yaml", File.ReadAllText(path));
    }

    [Fact]
    public void Html_export_writes_summary_and_details()
    {
        string path = Export(new HtmlDiagnosticReportExporter(), CreateContext(), CreateDocument());

        Verify("html", File.ReadAllText(path));
    }

    [Fact]
    public void Html_export_states_that_there_are_no_results()
    {
        string path = Export(new HtmlDiagnosticReportExporter(), CreateContext(), CreateDocument(withItems: false));

        Verify("html", File.ReadAllText(path));
    }

    private static void Verify(
        string extension,
        string actual,
        [System.Runtime.CompilerServices.CallerMemberName] string testName = "") =>
        Snapshot.Match(SnapshotDirectory, nameof(DiagnosticReportExporterTests), extension, actual, testName);

    private string Export(
        IDiagnosticReportExporter exporter,
        DiagnosticReportExportContext context,
        DiagnosticReportExportDocument document)
    {
        string path = Path.Combine(_outputDirectory, "report" + exporter.Extension);
        exporter.Export(path, context, document);
        return path;
    }

    // The invariant culture keeps date and number text identical on .NET Framework and .NET,
    // whose regional data differ.
    private static DiagnosticReportExportContext CreateContext(string listSeparator = ",")
    {
        CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.TextInfo.ListSeparator = listSeparator;
        return new DiagnosticReportExportContext
        {
            Culture = culture,
            UiCulture = new CultureInfo("en"),
            SeverityHeader = "Severity",
            CodeHeader = "Code",
            MessageHeader = "Message",
            DocumentHeader = "Document",
            CreatedHeader = "Created",
            ReportTitle = "Diagnostic report",
            GeneratedLabel = "Generated",
            TotalLabel = "Total",
            SummaryByCodeTitle = "Summary by code",
            CountHeader = "Count",
            DetailsTitle = "Details",
            NoResultsText = "No results",
            ErrorText = "Errors",
            WarningText = "Warnings",
            MessageText = "Messages"
        };
    }

    private static DiagnosticReportExportDocument CreateDocument(bool withItems = true)
    {
        TimeSpan offset = TimeSpan.FromHours(5);
        DiagnosticReportExportItem[] items = withItems
            ?
            [
                new(
                    "Error", "Error", "ELM002", "Wall \"W-1\", level 1\nsecond line", "Project A",
                    new DateTimeOffset(2026, 10, 1, 9, 30, 0, offset),
                    false, null, "101", ["201", "202"]),
                new(
                    "Warning", "Warning", "ELM001", "Plain message; with semicolon", "Project A",
                    new DateTimeOffset(2026, 10, 1, 9, 31, 5, offset),
                    true, "Use ELM002", null, []),
                new(
                    "Warning", "Warning", "ELM001", "Another", "Project A",
                    new DateTimeOffset(2026, 10, 1, 9, 32, 0, offset),
                    false, null, "102", [])
            ]
            : [];

        return new DiagnosticReportExportDocument(
            "1.0",
            new DiagnosticReportExportMetadata(
                new DateTimeOffset(2026, 10, 1, 10, 0, 0, offset),
                "2025",
                "25.0.2.419",
                "1.8.0",
                "Project A",
                "Document",
                new DiagnosticReportExportFilters(null, ["Error", "Warning"], ["Active"]),
                TotalItemCount: 5,
                ExportedItemCount: items.Length),
            items);
    }
}
