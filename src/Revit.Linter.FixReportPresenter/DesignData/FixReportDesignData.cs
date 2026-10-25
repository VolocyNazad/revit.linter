#pragma warning disable S2325 // The XAML designer binds to instance members only, so design properties stay non-static.

using CommunityToolkit.Mvvm.Input;
using Revit.Linter.Behaviors;
using Revit.Linter.Localization;
using Revit.Linter.OpenedDocuments.Abstractions.Models;
using Revit.Linter.ReportMessaging;
using System.Windows.Data;
using System.Windows.Input;

namespace Revit.Linter.FixReportPresenter.DesignData;

/// <summary>Provides representative sample values for <see cref="Views.FixReportView"/> in the XAML designer.</summary>
internal sealed class FixReportDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.FixReportPresenter.ViewModels.FixReportViewModel";
    private const string SampleDocumentTitle = "Sample Project - Architecture.rvt";

    /// <summary>Initializes the sample fix rows and the collection view the grid renders.</summary>
    public FixReportDesignData()
    {
        string showElementToolTipFormat = Localized("showElement_toolTip");
        List<FixReportRowDesignData> rows =
        [
            new()
            {
                Code = "DOC001",
                MessageParts =
                [
                    new(ReportInlineType.Text, "Door "),
                    new(ReportInlineType.Hyperlink, "101", 1234567L),
                    new(ReportInlineType.Text, " has no mark; the fix sets it to 'D-101'."),
                ],
                ShowElementToolTipFormat = showElementToolTipFormat,
                DocumentTitle = SampleDocumentTitle,
                Created = DateTime.Now.AddHours(-2),
                CreatedText = "2 hours ago",
            },
            new()
            {
                Code = "PRM001",
                MessageParts =
                [
                    new(ReportInlineType.Text, "The required project parameter 'ADSK_ApartmentNumber' is missing."),
                ],
                ShowElementToolTipFormat = showElementToolTipFormat,
                DocumentTitle = SampleDocumentTitle,
                Created = DateTime.Now.AddDays(-1),
                CreatedText = "yesterday",
            },
        ];

        CollectionViewSource = new() { Source = rows };
        SelectedFixReport = rows[0];
        TargetDocumentTitle = SampleDocumentTitle;
    }

    /// <summary>Gets the opened-document selector shown in the filter popup.</summary>
    public OpenedDocumentsDesignData OpenedDocuments { get; } = new();

    /// <summary>Gets the collection view the grid reads its rows from.</summary>
    public CollectionViewSource CollectionViewSource { get; }

    /// <summary>Gets or sets the row the grid shows as selected.</summary>
    public FixReportRowDesignData? SelectedFixReport { get; set; }

    /// <summary>Gets or sets the document the table is filtered to; the sample title selects the first document.</summary>
    public string? TargetDocumentTitle { get; set; }

    /// <summary>Gets or sets the search query of the pane; empty shows the search hint.</summary>
    public string SearchField { get; set; } = string.Empty;

    /// <summary>Gets the localized search hint and tooltip title.</summary>
    public string SearchText => Localized("search_text");

    /// <summary>Gets the localized search tooltip description.</summary>
    public string SearchDescription => Localized("search_description");

    /// <summary>Gets the localized fix-count label of the toolbar.</summary>
    public string FixesText => Localized("fixes_text");

    /// <summary>Gets the localized documentation button text.</summary>
    public string OpenHelpText => Localized("openHelp_text");

    /// <summary>Gets the localized documentation tooltip description.</summary>
    public string OpenHelpDescription => Localized("openHelp_description");

    /// <summary>Gets the localized document filter button text.</summary>
    public string DocumentText => Localized("document_text");

    /// <summary>Gets the localized document filter tooltip description.</summary>
    public string DocumentDescription => Localized("document_description");

    /// <summary>Gets the localized additional-filters button text.</summary>
    public string MoreFiltersTitle => Localized("moreFilters_title");

    /// <summary>Gets the localized additional-filters tooltip description.</summary>
    public string MoreFiltersDescription => Localized("moreFilters_description");

    /// <summary>Gets the localized code column header.</summary>
    public string CodeHeader => Localized("code_header");

    /// <summary>Gets the localized message column header.</summary>
    public string MessageHeader => Localized("message_header");

    /// <summary>Gets the localized document column header.</summary>
    public string DocumentHeader => Localized("document_header");

    /// <summary>Gets the localized created column header.</summary>
    public string CreatedHeader => Localized("created_header");

    /// <summary>Gets the command that initializes the pane when the view is loaded.</summary>
    public ICommand InitializeCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that opens the pane documentation.</summary>
    public ICommand OpenHelpCommand { get; } = new RelayCommand(() => { });

    private static string Localized(string key) => LocalizationResourceReader.GetString(ResourceBaseName, key);
}

/// <summary>Provides one representative fix-report row for the XAML designer.</summary>
internal sealed class FixReportRowDesignData : IHasDiagnosticCode
{
    /// <summary>Gets the diagnostic code of the fix.</summary>
    public required string Code { get; init; }

    /// <summary>Gets the rendered parts of the fix message.</summary>
    public required IReadOnlyList<ReportTextPart> MessageParts { get; init; }

    /// <summary>Gets the localized format of a hyperlink tooltip.</summary>
    public required string ShowElementToolTipFormat { get; init; }

    /// <summary>Gets the title of the document the fix belongs to.</summary>
    public required string DocumentTitle { get; init; }

    /// <summary>Gets the creation time of the fix.</summary>
    public required DateTime Created { get; init; }

    /// <summary>Gets the relative creation time of the fix.</summary>
    public required string CreatedText { get; init; }

    /// <summary>Gets the plain message text used when the grid sorts the column.</summary>
    public string MessageText => string.Concat(MessageParts.Select(part => part.Text));

    /// <summary>Gets the command that shows a linked element in the model.</summary>
    public ICommand AccentElementCommand { get; } = new RelayCommand<object?>(_ => { });
}

/// <summary>Provides the document selector entries of the fix-report pane for the XAML designer.</summary>
internal sealed class OpenedDocumentsDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.OpenedDocuments.ViewModels.OpenedDocumentsViewModel";

    /// <summary>Gets the documents available in the selector, starting with the entry for all documents.</summary>
    public IReadOnlyList<OpenedDocument> Documents { get; } =
    [
        new OpenedDocument
        {
            Title = string.Empty,
            DisplayName = LocalizationResourceReader.GetString(ResourceBaseName, "allDocuments_text"),
        },
        new OpenedDocument { Title = "Sample Project - Architecture.rvt", DisplayName = "Sample Project - Architecture.rvt" },
        new OpenedDocument { Title = "Sample Project - Structure.rvt", DisplayName = "Sample Project - Structure.rvt" },
    ];
}
