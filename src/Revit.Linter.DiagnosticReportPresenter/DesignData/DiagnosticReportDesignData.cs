#pragma warning disable S2325 // The XAML designer binds to instance members only, so design properties stay non-static.

using CommunityToolkit.Mvvm.Input;
using Revit.Linter.Localization;
using Revit.Linter.OpenedDocuments.Abstractions.Models;
using Revit.Linter.ReportMessaging;
using Revit.Linter.ThemeManaging.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Revit.Linter.DiagnosticReportPresenter.DesignData;

/// <summary>Provides representative sample values for <see cref="Views.DiagnosticReportView"/> in the XAML designer.</summary>
internal sealed class DiagnosticReportDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.DiagnosticReportPresenter.ViewModels.DiagnosticReportViewModel";

    /// <summary>Initializes the column visibility popup with the localized headers of the table.</summary>
    public DiagnosticReportDesignData()
    {
        Columns = new(
        [
            new(CodeHeader),
            new(MessageHeader),
            new(DocumentHeader),
            new(CreatedHeader),
            new(TagsHeader),
        ]);
    }

    private static readonly string ShowElementToolTipTemplate =
        LocalizationResourceReader.GetString(ResourceBaseName, "showElement_toolTip");

    private static readonly string ExpandListToolTipTemplate =
        LocalizationResourceReader.GetString(ResourceBaseName, "expandList_toolTip");

    private static readonly IReadOnlyList<ReportTextPart> FoldedHiddenNames =
    [
        new ReportTextPart(ReportInlineType.Text, "• "),
        new ReportTextPart(ReportInlineType.Text, "Walls 1"),
        new ReportTextPart(ReportInlineType.LineBreak, "\n"),
        new ReportTextPart(ReportInlineType.Text, "• "),
        new ReportTextPart(ReportInlineType.Text, "Wall 2"),
        new ReportTextPart(ReportInlineType.LineBreak, "\n"),
        new ReportTextPart(ReportInlineType.Text, "• "),
        new ReportTextPart(ReportInlineType.Text, "Walls 3"),
        new ReportTextPart(ReportInlineType.LineBreak, "\n"),
        new ReportTextPart(ReportInlineType.Text, "• "),
        new ReportTextPart(ReportInlineType.Text, "Grids 1"),
        new ReportTextPart(ReportInlineType.LineBreak, "\n"),
        new ReportTextPart(ReportInlineType.Text, "• "),
        new ReportTextPart(ReportInlineType.Text, "Doors 1"),
    ];

    private static readonly IReadOnlyList<ReportTextPart> FoldedHiddenIds =
    [
        new ReportTextPart(ReportInlineType.Text, "• "),
        new ReportTextPart(ReportInlineType.Hyperlink, "1234567", "1234567"),
        new ReportTextPart(ReportInlineType.LineBreak, "\n"),
        new ReportTextPart(ReportInlineType.Text, "• "),
        new ReportTextPart(ReportInlineType.Hyperlink, "1234568", "1234568"),
        new ReportTextPart(ReportInlineType.LineBreak, "\n"),
        new ReportTextPart(ReportInlineType.Text, "• "),
        new ReportTextPart(ReportInlineType.Hyperlink, "1234569", "1234569"),
        new ReportTextPart(ReportInlineType.LineBreak, "\n"),
        new ReportTextPart(ReportInlineType.Text, "• "),
        new ReportTextPart(ReportInlineType.Hyperlink, "1234570", "1234570"),
    ];

    private static readonly IReadOnlyList<DiagnosticReportRowDesignData> Rows =
    [
        new DiagnosticReportRowDesignData
        {
            Severity = DiagnosticSeverity.Warning,
            Code = "SHRD0041",
            DocumentTitle = "Sample Project.rvt",
            IsObsolete = false,
            ObsoleteDescription = string.Empty,
            MessageParts =
            [
                new ReportTextPart(ReportInlineType.Text, "View 'Level 1' (1234567) has 137 hidden element(s): '"),
                new ReportTextPart(ReportInlineType.Text, "\u201CWalls 1\u201D, \u201CWall 2\u201D, \u201CWalls 3\u201D, "),
                new ReportTextPart(ReportInlineType.Expander, "+134", FoldedHiddenNames),
                new ReportTextPart(ReportInlineType.Text, "' (1234567, 1234568, 1234569"),
                new ReportTextPart(ReportInlineType.Expander, "+134", FoldedHiddenIds),
                new ReportTextPart(ReportInlineType.Text, "). Duration: 12 ms."),
            ],
            ExpandedListParts = FoldedHiddenNames,
            ShowElementToolTipFormat = ShowElementToolTipTemplate,
            ExpandListToolTip = ExpandListToolTipTemplate,
            Fixes =
            [
                new DiagnosticReportFixDesignData { Title = "Assign the next free mark" },
            ],
            VisualizationPipelines =
            [
                new DiagnosticReportVisualizationDesignData { Title = "Show in active view", IsActive = false },
            ],
            Created = new DateTime(2026, 10, 20, 11, 24, 0, DateTimeKind.Local),
            CreatedText = "2 hours ago",
            Tags = ["Doors", "Project"],
        },
        new DiagnosticReportRowDesignData
        {
            Severity = DiagnosticSeverity.Error,
            Code = "CLN101",
            DocumentTitle = "Sample Project.rvt",
            IsObsolete = true,
            ObsoleteDescription = "This finding was reported by the replaced CLN101 diagnostic.",
            MessageParts =
            [
                new ReportTextPart(ReportInlineType.Text, "Sheet number is used more than once: "),
                new ReportTextPart(ReportInlineType.Hyperlink, "A-101", "A-101"),
            ],
            ShowElementToolTipFormat = ShowElementToolTipTemplate,
            ExpandListToolTip = ExpandListToolTipTemplate,
            HasActiveVisualization = true,
            VisualizationPipelines =
            [
                new DiagnosticReportVisualizationDesignData { Title = "Show in active view", IsActive = true },
                new DiagnosticReportVisualizationDesignData { Title = "Isolate in a section box", IsActive = false },
            ],
            Created = new DateTime(2026, 10, 19, 16, 5, 0, DateTimeKind.Local),
            CreatedText = "yesterday",
            Tags = ["Sheets"],
        },
        new DiagnosticReportRowDesignData
        {
            Severity = DiagnosticSeverity.Message,
            Code = "VOL101",
            DocumentTitle = "Family.rfa",
            IsObsolete = false,
            ObsoleteDescription = string.Empty,
            MessageParts =
            [
                new ReportTextPart(ReportInlineType.Text, "Family parameter 'Height' is not used by any dimension."),
            ],
            ShowElementToolTipFormat = ShowElementToolTipTemplate,
            ExpandListToolTip = ExpandListToolTipTemplate,
            Fixes =
            [
                new DiagnosticReportFixDesignData { Title = "Remove the unused parameter" },
                new DiagnosticReportFixDesignData { Title = "Keep the parameter" },
            ],
            VisualizationPipelines =
            [
                new DiagnosticReportVisualizationDesignData { Title = "Show in active view", IsActive = false },
            ],
            Created = new DateTime(2026, 10, 17, 9, 40, 0, DateTimeKind.Local),
            CreatedText = "3 days ago",
            Tags = ["Families", "Family"],
        },
    ];

    /// <summary>Gets the command that initializes the report when the pane is shown.</summary>
    public ICommand InitializeCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that opens the documentation of the pane.</summary>
    public ICommand OpenHelpCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that selects and shows the previous finding.</summary>
    public ICommand ShowPreviousVisualizationCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that selects and shows the next finding.</summary>
    public ICommand ShowNextVisualizationCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that shows the current finding.</summary>
    public ICommand ShowCurrentVisualizationCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that exports the displayed findings.</summary>
    public ICommand ExportCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that selects a row and shows its only visualization.</summary>
    public ICommand SelectAndShowPrimaryVisualizationCommand { get; } = new RelayCommand<object?>(_ => { });

    /// <summary>Gets the command that searches for a tag clicked in a row.</summary>
    public ICommand FilterByTagCommand { get; } = new RelayCommand<string?>(_ => { });

    /// <summary>Gets the text of the navigation counter.</summary>
    public string NavigationPositionText => "2 / 12";

    /// <summary>Gets or sets the text of the search box.</summary>
    public string SearchField { get; set; } = "mark";

    /// <summary>Gets or sets the title of the document whose findings are shown.</summary>
    public string TargetDocumentTitle { get; set; } = "Sample Project.rvt";

    /// <summary>Gets or sets the selected finding.</summary>
    public DiagnosticReportRowDesignData? SelectedReport { get; set; } = Rows[0];

    /// <summary>Gets the document selector shown in the filter popup.</summary>
    public OpenedDocumentsDesignData OpenedDocuments { get; } = new();

    /// <summary>Gets the severity filters shown in the filter popup.</summary>
    public IEnumerable<DiagnosticReportFilterDesignData> SeverityFilters { get; } =
    [
        new DiagnosticReportFilterDesignData(true, 4, DiagnosticSeverityLocalizations.GetString("Message")),
        new DiagnosticReportFilterDesignData(true, 3, DiagnosticSeverityLocalizations.GetString("Warning")),
        new DiagnosticReportFilterDesignData(true, 5, DiagnosticSeverityLocalizations.GetString("Error")),
    ];

    /// <summary>Gets the state filters shown in the filter popup.</summary>
    public IEnumerable<DiagnosticReportFilterDesignData> Filters { get; } =
    [
        new DiagnosticReportFilterDesignData(true, 10, DiagnosticReportFilterLocalizations.GetString("Actual")),
        new DiagnosticReportFilterDesignData(true, 2, DiagnosticReportFilterLocalizations.GetString("Obsolete")),
    ];

    /// <summary>Gets the collection view the table binds to, filled with representative rows.</summary>
    public CollectionViewSource CollectionViewSource { get; } = new() { Source = Rows };

    /// <summary>Gets the localized name of the document selector.</summary>
    public string DocumentText => LocalizationResourceReader.GetString(ResourceBaseName, "document_text");

    /// <summary>Gets the localized document selector tooltip description.</summary>
    public string DocumentDescription => LocalizationResourceReader.GetString(ResourceBaseName, "document_description");

    /// <summary>Gets the localized text of the export action.</summary>
    public string ExportReportText => LocalizationResourceReader.GetString(ResourceBaseName, "exportReport_text");

    /// <summary>Gets the localized export tooltip description.</summary>
    public string ExportReportDescription => LocalizationResourceReader.GetString(ResourceBaseName, "exportReport_description");

    /// <summary>Gets the localized placeholder of the search box.</summary>
    public string SearchText => LocalizationResourceReader.GetString(ResourceBaseName, "search_text");

    /// <summary>Gets the localized search tooltip description.</summary>
    public string SearchDescription => LocalizationResourceReader.GetString(ResourceBaseName, "search_description");

    /// <summary>Gets the localized text of the reports counter.</summary>
    public string ReportsText => LocalizationResourceReader.GetString(ResourceBaseName, "reports_text");

    /// <summary>Gets the localized documentation tooltip title.</summary>
    public string OpenHelpText => LocalizationResourceReader.GetString(ResourceBaseName, "openHelp_text");

    /// <summary>Gets the localized documentation tooltip description.</summary>
    public string OpenHelpDescription => LocalizationResourceReader.GetString(ResourceBaseName, "openHelp_description");

    /// <summary>Gets the localized text of the previous finding action.</summary>
    public string ShowPreviousText => LocalizationResourceReader.GetString(ResourceBaseName, "showPrevious_text");

    /// <summary>Gets the localized description of the previous finding action.</summary>
    public string ShowPreviousDescription => LocalizationResourceReader.GetString(ResourceBaseName, "showPrevious_description");

    /// <summary>Gets the localized text of the next finding action.</summary>
    public string ShowNextText => LocalizationResourceReader.GetString(ResourceBaseName, "showNext_text");

    /// <summary>Gets the localized description of the next finding action.</summary>
    public string ShowNextDescription => LocalizationResourceReader.GetString(ResourceBaseName, "showNext_description");

    /// <summary>Gets the localized filter popup tooltip title.</summary>
    public string MoreFiltersTitle => LocalizationResourceReader.GetString(ResourceBaseName, "moreFilters_title");

    /// <summary>Gets the localized filter popup tooltip description.</summary>
    public string MoreFiltersDescription => LocalizationResourceReader.GetString(ResourceBaseName, "moreFilters_description");

    /// <summary>Gets the popup that hides and shows the data columns of the table.</summary>
    public TableColumnsDesignData Columns { get; }

    /// <summary>Gets the localized column popup tooltip title.</summary>
    public string ColumnsTitle => LocalizationResourceReader.GetString(ResourceBaseName, "columns_title");

    /// <summary>Gets the localized column popup tooltip description.</summary>
    public string ColumnsDescription => LocalizationResourceReader.GetString(ResourceBaseName, "columns_description");

    /// <summary>Gets the localized text of the show-all-columns action.</summary>
    public string ResetColumnsText => LocalizationResourceReader.GetString(ResourceBaseName, "resetColumns_text");

    /// <summary>Gets the localized header of the code column.</summary>
    public string CodeHeader => LocalizationResourceReader.GetString(ResourceBaseName, "code_header");

    /// <summary>Gets the localized header of the message column.</summary>
    public string MessageHeader => LocalizationResourceReader.GetString(ResourceBaseName, "message_header");

    /// <summary>Gets the localized header of the document column.</summary>
    public string DocumentHeader => LocalizationResourceReader.GetString(ResourceBaseName, "document_header");

    /// <summary>Gets the localized header of the created column.</summary>
    public string CreatedHeader => LocalizationResourceReader.GetString(ResourceBaseName, "created_header");

    /// <summary>Gets the localized header of the tags column.</summary>
    public string TagsHeader => LocalizationResourceReader.GetString(ResourceBaseName, "tags_header");

    /// <summary>Gets the localized tooltip description of the tags column.</summary>
    public string TagsDescription => LocalizationResourceReader.GetString(ResourceBaseName, "tags_description");

    /// <summary>Gets the localized tooltip title of the row documentation button.</summary>
    public string OpenDocumentationText => LocalizationResourceReader.GetString(ResourceBaseName, "openDocumentation_text");

    /// <summary>Gets the localized tooltip title of the row fix button.</summary>
    public string FixTitle => LocalizationResourceReader.GetString(ResourceBaseName, "fix_title");

    /// <summary>Gets the localized tooltip description of the row fix button.</summary>
    public string FixDescription => LocalizationResourceReader.GetString(ResourceBaseName, "fix_description");

    /// <summary>Gets the localized tooltip title of the row visualization button.</summary>
    public string ShowVisualizationText => LocalizationResourceReader.GetString(ResourceBaseName, "showVisualization_text");

    /// <summary>Gets the localized tooltip description of the row visualization button.</summary>
    public string ShowVisualizationDescription => LocalizationResourceReader.GetString(ResourceBaseName, "showVisualization_description");
}

/// <summary>Provides a representative filter of <see cref="Views.DiagnosticReportView"/>.</summary>
internal sealed class DiagnosticReportFilterDesignData(bool isActive, int count, string label)
{
    /// <summary>Gets or sets whether the filter lets its rows through.</summary>
    public bool IsActive { get; set; } = isActive;

    /// <summary>Gets the number of rows matching the filter.</summary>
    public int Count { get; } = count;

    /// <summary>Returns the counted display text of the filter.</summary>
    public override string ToString() => $"{Count} {label}";
}

/// <summary>Provides a representative finding row of <see cref="Views.DiagnosticReportView"/>.</summary>
internal sealed class DiagnosticReportRowDesignData
{
    /// <summary>Gets the severity of the finding.</summary>
    public required DiagnosticSeverity Severity { get; init; }

    /// <summary>Gets the localized text of the severity.</summary>
    public string SeverityText => DiagnosticSeverityLocalizations.GetString(Severity.ToString());

    /// <summary>Gets the code of the diagnostic that reported the finding.</summary>
    public required string Code { get; init; }

    /// <summary>Gets the title of the document the finding belongs to.</summary>
    public required string DocumentTitle { get; init; }

    /// <summary>Gets whether the finding is obsolete.</summary>
    public bool IsObsolete { get; init; }

    /// <summary>Gets the explanation shown when the finding is obsolete.</summary>
    public string ObsoleteDescription { get; init; } = string.Empty;

    /// <summary>Gets the rendered parts of the finding message.</summary>
    public required IReadOnlyList<ReportTextPart> MessageParts { get; init; }

    /// <summary>Gets the complete parts of the list the expander opened, or <see langword="null"/> until one is opened.</summary>
    public IReadOnlyList<ReportTextPart>? ExpandedListParts { get; init; }

    /// <summary>Gets or sets whether the expanded list popup is open.</summary>
    public bool IsListExpanded { get; set; }

    /// <summary>Gets the plain text of the finding message, used to sort the column.</summary>
    public string MessageText => ReportMessageParser.BuildPlainText(MessageParts);

    /// <summary>Gets the composite format string of an element link tooltip.</summary>
    public required string ShowElementToolTipFormat { get; init; }

    /// <summary>Gets the localized tooltip of the control that reveals a folded list.</summary>
    public required string ExpandListToolTip { get; init; }

    /// <summary>Gets the command that opens a folded list with all of its items.</summary>
    public ICommand ExpandListCommand { get; } = new RelayCommand<object?>(_ => { });

    /// <summary>Gets the command that marks the expanded list popup as closed.</summary>
    public ICommand CollapseListCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the fixes and ignore actions of the finding.</summary>
    public IReadOnlyList<DiagnosticReportFixDesignData> Fixes { get; init; } = [];

    /// <summary>Gets whether the finding offers at least one fix.</summary>
    public bool HasFixes => Fixes.Count > 0;

    /// <summary>Gets whether the finding offers more than one fix.</summary>
    public bool HasMultipleFixes => Fixes.Count > 1;

    /// <summary>Gets the visualizations of the finding.</summary>
    public IReadOnlyList<DiagnosticReportVisualizationDesignData> VisualizationPipelines { get; init; } = [];

    /// <summary>Gets whether the finding offers at least one visualization.</summary>
    public bool HasVisualizationPipelines => VisualizationPipelines.Count > 0;

    /// <summary>Gets whether the finding offers more than one visualization.</summary>
    public bool HasMultipleVisualizationPipelines => VisualizationPipelines.Count > 1;

    /// <summary>Gets whether a visualization of the finding is currently applied.</summary>
    public bool HasActiveVisualization { get; init; }

    /// <summary>Gets the moment the finding was reported.</summary>
    public DateTime Created { get; init; }

    /// <summary>Gets the localized text of <see cref="Created"/>.</summary>
    public required string CreatedText { get; init; }

    /// <summary>Gets the tags of the diagnostic that reported the finding.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Gets the tags as one text, used to sort the column.</summary>
    public string TagsText => string.Join(", ", Tags);

    /// <summary>Gets the command that runs the only fix of the finding.</summary>
    public ICommand ShowPrimaryFixCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that accents an element linked in the message.</summary>
    public ICommand AccentElementCommand { get; } = new RelayCommand<object?>(_ => { });

    /// <summary>Gets the command that opens the documentation of the diagnostic.</summary>
    public ICommand OpenDocumentationCommand { get; } = new RelayCommand(() => { });
}

/// <summary>Provides a representative fix action of a finding row.</summary>
internal sealed class DiagnosticReportFixDesignData
{
    /// <summary>Gets the title of the fix.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the command that runs the fix.</summary>
    public ICommand FixCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the icon of the fix.</summary>
    public object Icon { get; } = new PathIcon { Kind = IconKind.Lightbulb };
}

/// <summary>Provides a representative visualization action of a finding row.</summary>
internal sealed class DiagnosticReportVisualizationDesignData
{
    /// <summary>Gets the title of the visualization.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the command that applies the visualization from the menu.</summary>
    public ICommand ShowFromMenuCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets whether the visualization is currently applied.</summary>
    public bool IsActive { get; init; }
}

/// <summary>Provides representative values for the document selector of <see cref="Views.DiagnosticReportView"/>.</summary>
internal sealed class OpenedDocumentsDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.OpenedDocuments.ViewModels.OpenedDocumentsViewModel";

    /// <summary>Gets the selectable documents, starting with the item that represents all documents.</summary>
    public IReadOnlyList<OpenedDocument> Documents { get; } =
    [
        new OpenedDocument
        {
            Title = string.Empty,
            DisplayName = LocalizationResourceReader.GetString(ResourceBaseName, "allDocuments_text"),
        },
        new OpenedDocument { Title = "Sample Project.rvt", DisplayName = "Sample Project.rvt" },
        new OpenedDocument { Title = "Family.rfa", DisplayName = "Family.rfa" },
    ];
}
