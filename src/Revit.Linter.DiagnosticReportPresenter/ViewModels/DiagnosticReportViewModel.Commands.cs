// Clipboard, tag filtering and report export commands.

using CommunityToolkit.Mvvm.Input;
using Revit.Linter.DiagnosticReportExporting.Abstractions.Models;
using Revit.Linter.Presentation.ViewModels;
using Revit.Linter.Localization;
using System.Globalization;
using System.Reflection;
using System.Windows;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

internal sealed partial class DiagnosticReportViewModel
{
    #region [CopyToClipboard] Command - Copy text to clipboard

    /// <summary> Copy text to clipboard </summary>
    [RelayCommand]
    private void CopyToClipboard(object item) => Clipboard.SetText(item.ToString() ?? string.Empty);

    #endregion

    #region [FilterByTag] Command - Filter by tag

    /// <summary>
    /// Puts a row tag into the search box, so the list shows the findings that carry it.
    /// </summary>
    /// <param name="tag">The tag clicked in a row.</param>
    [RelayCommand]
    private void FilterByTag(string? tag)
    {
        if (tag is null || string.IsNullOrWhiteSpace(tag)) return;
        SearchField = tag;
    }

    #endregion

    #region [Export] Command - Export

    /// <summary> Export </summary>
    [RelayCommand]
    private void Export()
    {
        List<DiagnosticReportExportItem> items = (CollectionViewSource?.View
            .Cast<object>()
            .OfType<DiagnosticReportItemViewModel>() ?? [])
            .Select(item => new DiagnosticReportExportItem(
                item.Severity.ToString(),
                item.SeverityText,
                item.Code,
                item.MessageText,
                item.DocumentTitle,
                new DateTimeOffset(item.Created),
                item.IsObsolete,
                string.IsNullOrWhiteSpace(item.ObsoleteDescription) ? null : item.ObsoleteDescription,
                item.TargetElementId?.ToString(),
                item.TargetDependencyElementIds
                    .Select(elementId => elementId.ToString())
                    .ToArray(),
                item.Tags))
            .ToList();

        Assembly assembly = typeof(DiagnosticReportViewModel).Assembly;
        DiagnosticReportExportLabels labels = new()
        {
            Culture = CultureInfo.CurrentCulture,
            UiCulture = CultureInfo.CurrentUICulture,
            SeverityHeader = SeverityHeader,
            CodeHeader = CodeHeader,
            MessageHeader = MessageHeader,
            DocumentHeader = DocumentHeader,
            CreatedHeader = CreatedHeader,
            TagsHeader = TagsHeader,
            ReportTitle = HtmlReportTitle,
            GeneratedLabel = HtmlGeneratedLabel,
            TotalLabel = HtmlTotalLabel,
            SummaryByCodeTitle = HtmlSummaryByCodeTitle,
            CountHeader = HtmlCountHeader,
            DetailsTitle = HtmlDetailsTitle,
            NoResultsText = HtmlNoResultsText,
            ErrorText = DiagnosticSeverityLocalizations.GetString(DiagnosticSeverity.Error.ToString()),
            WarningText = DiagnosticSeverityLocalizations.GetString(DiagnosticSeverity.Warning.ToString()),
            MessageText = DiagnosticSeverityLocalizations.GetString(DiagnosticSeverity.Message.ToString()),
            AllDocumentsText = HtmlAllDocumentsText
        };
        DiagnosticReportExportRequest request = new(
            items,
            Collection.Count,
            string.IsNullOrWhiteSpace(TargetDocumentTitle) ? null : TargetDocumentTitle,
            string.IsNullOrWhiteSpace(SearchField) ? null : SearchField,
            SeverityFilters
                .OfType<DiagnosticSeverityFilterViewModel>()
                .Where(filter => filter.IsActive)
                .Select(filter => filter.Value.ToString())
                .ToArray(),
            Filters
                .Where(filter => filter.IsActive)
                .Select(filter => filter is FilterViewModel<DiagnosticReportItemViewModel, bool> state
                    && state.Value
                    ? "Obsolete"
                    : "Actual")
                .ToArray(),
            labels,
            _revitContext.ControlledApplication?.VersionNumber ?? string.Empty,
            _revitContext.ControlledApplication?.VersionBuild ?? string.Empty,
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? string.Empty,
            DateTimeOffset.Now);
        DiagnosticReportExportResult result = _exportService.Export(request);
        if (result.Completed && result.Extension is not null)
            _activityStream.Publish(new DiagnosticReportExportedActivity(result.Extension));
    }

    #endregion
}

