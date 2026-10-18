using System.Globalization;

namespace Revit.Linter.DiagnosticReportExporting.Abstractions.Models;

/// <summary>
/// The localized texts the export orchestration renders the report with.
/// </summary>
/// <remarks>
/// The labels are supplied by the caller so this module stays free of localization infrastructure;
/// every text is the same one the report presenter shows.
/// </remarks>
public sealed class DiagnosticReportExportLabels
{
    /// <summary>Gets the culture used to format dates and numbers.</summary>
    public required CultureInfo Culture { get; init; }

    /// <summary>Gets the culture used to pick the HTML document language.</summary>
    public required CultureInfo UiCulture { get; init; }

    /// <summary>Gets the severity column header.</summary>
    public required string SeverityHeader { get; init; }

    /// <summary>Gets the diagnostic code column header.</summary>
    public required string CodeHeader { get; init; }

    /// <summary>Gets the message column header.</summary>
    public required string MessageHeader { get; init; }

    /// <summary>Gets the document column header.</summary>
    public required string DocumentHeader { get; init; }

    /// <summary>Gets the creation date column header.</summary>
    public required string CreatedHeader { get; init; }

    /// <summary>Gets the tags column header.</summary>
    public required string TagsHeader { get; init; }

    /// <summary>Gets the HTML report title.</summary>
    public required string ReportTitle { get; init; }

    /// <summary>Gets the HTML generated label.</summary>
    public required string GeneratedLabel { get; init; }

    /// <summary>Gets the HTML total label.</summary>
    public required string TotalLabel { get; init; }

    /// <summary>Gets the HTML summary-by-code title.</summary>
    public required string SummaryByCodeTitle { get; init; }

    /// <summary>Gets the HTML count column header.</summary>
    public required string CountHeader { get; init; }

    /// <summary>Gets the HTML details title.</summary>
    public required string DetailsTitle { get; init; }

    /// <summary>Gets the HTML text shown when there are no results.</summary>
    public required string NoResultsText { get; init; }

    /// <summary>Gets the localized error severity name.</summary>
    public required string ErrorText { get; init; }

    /// <summary>Gets the localized warning severity name.</summary>
    public required string WarningText { get; init; }

    /// <summary>Gets the localized message severity name.</summary>
    public required string MessageText { get; init; }

    /// <summary>Gets the text used as the document title when every open document is exported.</summary>
    public required string AllDocumentsText { get; init; }
}
