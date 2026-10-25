#pragma warning disable S2325 // The XAML designer binds to instance members only, so design properties stay non-static.

using CommunityToolkit.Mvvm.Input;
using Revit.Linter.Localization;
using System.Windows.Data;
using System.Windows.Input;

namespace Revit.Linter.DiagnosticListPresenter.DesignData;

/// <summary>Provides representative sample values for <see cref="Views.DiagnosticListView"/> in the XAML designer.</summary>
internal sealed class DiagnosticListDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.DiagnosticListPresenter.ViewModels.DiagnosticListViewModel";

    private static readonly IReadOnlyList<DiagnosticRowDesignData> Rows =
    [
        new DiagnosticRowDesignData
        {
            Code = "TOUR001",
            Description = "Wall has no fire rating assigned",
            Severity = DiagnosticSeverity.Warning,
            IsActive = true,
            HasConfiguration = true,
            Tags = ["Tour", "Architecture"],
            ObsoleteDescription = string.Empty,
        },
        new DiagnosticRowDesignData
        {
            Code = "STDR201",
            Description = "Door has no mark parameter",
            Severity = DiagnosticSeverity.Error,
            IsActive = true,
            HasConfiguration = true,
            Tags = ["Doors", "Project"],
            ObsoleteDescription = string.Empty,
        },
        new DiagnosticRowDesignData
        {
            Code = "CLN101",
            Description = "Duplicate sheets in the project",
            Severity = DiagnosticSeverity.Message,
            IsActive = false,
            HasConfiguration = false,
            Tags = ["Sheets"],
            IsObsolete = true,
            ObsoleteDescription = "This diagnostic was replaced by STDR201.",
        },
    ];

    /// <summary>Gets the command that loads the catalog when the pane is shown.</summary>
    public ICommand InitializeCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that opens the documentation of the pane.</summary>
    public ICommand OpenHelpCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that opens the configuration folder.</summary>
    public ICommand OpenConfigurationFolderCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that restores the saved check settings.</summary>
    public ICommand ResetSettingsCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that turns on every diagnostic.</summary>
    public ICommand CheckAllCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that turns off every diagnostic.</summary>
    public ICommand UncheckAllCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that turns on the visible diagnostics.</summary>
    public ICommand CheckVisibleCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that turns off the visible diagnostics.</summary>
    public ICommand UncheckVisibleCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that inverts every diagnostic.</summary>
    public ICommand InvertAllCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that inverts the visible diagnostics.</summary>
    public ICommand InvertVisibleCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that searches for a tag clicked in a row.</summary>
    public ICommand FilterByTagCommand { get; } = new RelayCommand<string?>(_ => { });

    /// <summary>Gets or sets the text of the search box.</summary>
    public string SearchField { get; set; } = "fire";

    /// <summary>Gets the target-type filters shown in the filter popup.</summary>
    public IEnumerable<DiagnosticFilterDesignData> Filters { get; } =
    [
        new DiagnosticFilterDesignData(true, 2, DiagnosticTargetTypeLocalizations.GetString("Element")),
        new DiagnosticFilterDesignData(true, 1, DiagnosticTargetTypeLocalizations.GetString("Document")),
    ];

    /// <summary>Gets the collection view the table binds to, filled with representative rows.</summary>
    public CollectionViewSource CollectionViewSource { get; } = new() { Source = Rows };

    /// <summary>Gets the localized text of the diagnostics counter.</summary>
    public string DiagnosticsText => LocalizationResourceReader.GetString(ResourceBaseName, "diagnostics_text");

    /// <summary>Gets the localized placeholder of the search box.</summary>
    public string SearchText => LocalizationResourceReader.GetString(ResourceBaseName, "search_text");

    /// <summary>Gets the localized search tooltip description.</summary>
    public string SearchDescription => LocalizationResourceReader.GetString(ResourceBaseName, "search_description");

    /// <summary>Gets the localized documentation tooltip title.</summary>
    public string OpenHelpText => LocalizationResourceReader.GetString(ResourceBaseName, "openHelp_text");

    /// <summary>Gets the localized documentation tooltip description.</summary>
    public string OpenHelpDescription => LocalizationResourceReader.GetString(ResourceBaseName, "openHelp_description");

    /// <summary>Gets the localized configuration folder tooltip title.</summary>
    public string OpenConfigurationFolderTitle => LocalizationResourceReader.GetString(ResourceBaseName, "openConfigurationFolder_title");

    /// <summary>Gets the localized configuration folder tooltip description.</summary>
    public string OpenConfigurationFolderDescription => LocalizationResourceReader.GetString(ResourceBaseName, "openConfigurationFolder_description");

    /// <summary>Gets the localized reset settings tooltip title.</summary>
    public string ResetSettingsText => LocalizationResourceReader.GetString(ResourceBaseName, "resetSettings_text");

    /// <summary>Gets the localized reset settings tooltip description.</summary>
    public string ResetSettingsDescription => LocalizationResourceReader.GetString(ResourceBaseName, "resetSettings_description");

    /// <summary>Gets the localized filter popup tooltip title.</summary>
    public string MoreFiltersTitle => LocalizationResourceReader.GetString(ResourceBaseName, "moreFilters_title");

    /// <summary>Gets the localized filter popup tooltip description.</summary>
    public string MoreFiltersDescription => LocalizationResourceReader.GetString(ResourceBaseName, "moreFilters_description");

    /// <summary>Gets the localized text of the check all action.</summary>
    public string CheckAllText => LocalizationResourceReader.GetString(ResourceBaseName, "checkAll_text");

    /// <summary>Gets the localized description of the check all action.</summary>
    public string CheckAllDescription => LocalizationResourceReader.GetString(ResourceBaseName, "checkAll_description");

    /// <summary>Gets the localized text of the uncheck all action.</summary>
    public string UncheckAllText => LocalizationResourceReader.GetString(ResourceBaseName, "uncheckAll_text");

    /// <summary>Gets the localized description of the uncheck all action.</summary>
    public string UncheckAllDescription => LocalizationResourceReader.GetString(ResourceBaseName, "uncheckAll_description");

    /// <summary>Gets the localized text of the check visible action.</summary>
    public string CheckVisibleText => LocalizationResourceReader.GetString(ResourceBaseName, "checkVisible_text");

    /// <summary>Gets the localized description of the check visible action.</summary>
    public string CheckVisibleDescription => LocalizationResourceReader.GetString(ResourceBaseName, "checkVisible_description");

    /// <summary>Gets the localized text of the uncheck visible action.</summary>
    public string UncheckVisibleText => LocalizationResourceReader.GetString(ResourceBaseName, "uncheckVisible_text");

    /// <summary>Gets the localized description of the uncheck visible action.</summary>
    public string UncheckVisibleDescription => LocalizationResourceReader.GetString(ResourceBaseName, "uncheckVisible_description");

    /// <summary>Gets the localized text of the invert all action.</summary>
    public string InvertAllText => LocalizationResourceReader.GetString(ResourceBaseName, "invertAll_text");

    /// <summary>Gets the localized description of the invert all action.</summary>
    public string InvertAllDescription => LocalizationResourceReader.GetString(ResourceBaseName, "invertAll_description");

    /// <summary>Gets the localized text of the invert visible action.</summary>
    public string InvertVisibleText => LocalizationResourceReader.GetString(ResourceBaseName, "invertVisible_text");

    /// <summary>Gets the localized description of the invert visible action.</summary>
    public string InvertVisibleDescription => LocalizationResourceReader.GetString(ResourceBaseName, "invertVisible_description");

    /// <summary>Gets the localized header of the active column.</summary>
    public string IsActiveHeader => LocalizationResourceReader.GetString(ResourceBaseName, "isActive_header");

    /// <summary>Gets the localized tooltip description of the active column.</summary>
    public string IsActiveDescription => LocalizationResourceReader.GetString(ResourceBaseName, "isActive_description");

    /// <summary>Gets the localized header of the code column.</summary>
    public string CodeHeader => LocalizationResourceReader.GetString(ResourceBaseName, "code_header");

    /// <summary>Gets the localized header of the severity column.</summary>
    public string SeverityHeader => LocalizationResourceReader.GetString(ResourceBaseName, "severity_header");

    /// <summary>Gets the localized tooltip description of the severity column.</summary>
    public string SeverityDescription => LocalizationResourceReader.GetString(ResourceBaseName, "severity_description");

    /// <summary>Gets the localized header of the description column.</summary>
    public string DescriptionHeader => LocalizationResourceReader.GetString(ResourceBaseName, "description_header");

    /// <summary>Gets the localized header of the tags column.</summary>
    public string TagsHeader => LocalizationResourceReader.GetString(ResourceBaseName, "tags_header");

    /// <summary>Gets the localized tooltip description of the tags column.</summary>
    public string TagsDescription => LocalizationResourceReader.GetString(ResourceBaseName, "tags_description");

    /// <summary>Gets the localized tooltip title of the row documentation button.</summary>
    public string OpenDocumentationText => LocalizationResourceReader.GetString(ResourceBaseName, "openDocumentation_text");

    /// <summary>Gets the localized tooltip title of the row configuration button.</summary>
    public string OpenConfigurationTitleText => LocalizationResourceReader.GetString(ResourceBaseName, "openConfigurationTitle_text");

    /// <summary>Gets the localized tooltip description of the row configuration button.</summary>
    public string OpenConfigurationDescriptionText => LocalizationResourceReader.GetString(ResourceBaseName, "openConfigurationDescription_text");
}

/// <summary>Provides a representative target-type filter of <see cref="Views.DiagnosticListView"/>.</summary>
internal sealed class DiagnosticFilterDesignData(bool isActive, int count, string label)
{
    /// <summary>Gets or sets whether the filter lets its rows through.</summary>
    public bool IsActive { get; set; } = isActive;

    /// <summary>Gets the number of rows matching the filter.</summary>
    public int Count { get; } = count;

    /// <summary>Returns the counted display text of the filter.</summary>
    public override string ToString() => $"{Count} {label}";
}

/// <summary>Provides a representative diagnostic row of <see cref="Views.DiagnosticListView"/>.</summary>
internal sealed class DiagnosticRowDesignData
{
    private const string ResourceBaseName =
        "Revit.Linter.Localization.DiagnosticListPresenter.ViewModels.DiagnosticListViewModel";

    /// <summary>Gets the code of the diagnostic.</summary>
    public required string Code { get; init; }

    /// <summary>Gets the description of the diagnostic.</summary>
    public required string Description { get; init; }

    /// <summary>Gets or sets the severity of the diagnostic.</summary>
    public required DiagnosticSeverity Severity { get; set; }

    /// <summary>Gets or sets whether the diagnostic is turned on.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets whether the diagnostic has a configuration file.</summary>
    public bool HasConfiguration { get; init; }

    /// <summary>Gets the tags of the diagnostic.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Gets the tags as one text, used to sort the column.</summary>
    public string TagsText => string.Join(", ", Tags);

    /// <summary>Gets whether the diagnostic is obsolete.</summary>
    public bool IsObsolete { get; init; }

    /// <summary>Gets the explanation shown when the diagnostic is obsolete.</summary>
    public string ObsoleteDescription { get; init; } = string.Empty;

    /// <summary>Gets the command that opens the configuration of the diagnostic.</summary>
    public ICommand OpenConfigurationCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that shows the Windows "Open with" dialog for the configuration.</summary>
    public ICommand OpenConfigurationWithCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that opens the configuration in Notepad.</summary>
    public ICommand OpenConfigurationInNotepadCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that shows the configuration selected in File Explorer.</summary>
    public ICommand RevealConfigurationInExplorerCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that opens the documentation of the diagnostic.</summary>
    public ICommand OpenDocumentationCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the localized menu label that opens the configuration with its associated application.</summary>
    public string OpenConfigurationText => LocalizationResourceReader.GetString(ResourceBaseName, "openConfiguration_text");

    /// <summary>Gets the localized menu label that shows the Windows "Open with" dialog.</summary>
    public string OpenConfigurationWithText => LocalizationResourceReader.GetString(ResourceBaseName, "openConfigurationWith_text");

    /// <summary>Gets the localized menu label that opens the configuration in Notepad.</summary>
    public string OpenConfigurationInNotepadText => LocalizationResourceReader.GetString(ResourceBaseName, "openConfigurationInNotepad_text");

    /// <summary>Gets the localized menu label that shows the configuration in File Explorer.</summary>
    public string RevealConfigurationInExplorerText => LocalizationResourceReader.GetString(ResourceBaseName, "revealConfigurationInExplorer_text");
}
