#pragma warning disable S2325 // The XAML designer binds to instance members only, so design properties stay non-static.

using CommunityToolkit.Mvvm.Input;
using Revit.Linter.IgnoreListPresenter.ViewModels;
using Revit.Linter.Localization;
using System.Windows.Data;
using System.Windows.Input;

namespace Revit.Linter.IgnoreListPresenter.DesignData;

/// <summary>Provides representative sample values for <see cref="Views.IgnoreListView"/> in the XAML designer.</summary>
internal sealed class IgnoreListDesignData
{
    private const string ResourceBaseName = "Revit.Linter.Localization.IgnoreListPresenter.ViewModels.IgnoreListViewModel";
    private const string SampleDocumentTitle = "Sample Project - Architecture.rvt";

    /// <summary>Initializes the sample ignore entries and the collection view the grid renders.</summary>
    public IgnoreListDesignData()
    {
        List<IgnoreListItemViewModel> rows =
        [
            new()
            {
                Code = "DOC001",
                ScopeText = Localized("scopeElement_text"),
                TargetText = "Basic Wall: Generic - 200mm",
                HasLiveElement = true,
                ElementIdValue = 1234567,
                DocumentTitle = SampleDocumentTitle,
            },
            new()
            {
                Code = "DOC002",
                ScopeText = Localized("scopeElement_text"),
                TargetText = Localized("deletedTarget_text"),
                IsMissing = true,
                DocumentTitle = SampleDocumentTitle,
            },
            new()
            {
                Code = "PRM001",
                ScopeText = Localized("scopeDocument_text"),
                TargetText = SampleDocumentTitle,
                DocumentTitle = SampleDocumentTitle,
            },
        ];

        CollectionViewSource = new() { Source = rows };
        SelectedEntry = rows[0];
    }

    /// <summary>Gets the collection view the grid reads its rows from.</summary>
    public CollectionViewSource CollectionViewSource { get; }

    /// <summary>Gets or sets the row the grid shows as selected.</summary>
    public IgnoreListItemViewModel? SelectedEntry { get; set; }

    /// <summary>Gets the document whose ignore list the pane shows.</summary>
    public string ActiveDocumentTitle => SampleDocumentTitle;

    /// <summary>Gets or sets the search query of the pane; empty shows the search hint.</summary>
    public string SearchField { get; set; } = string.Empty;

    /// <summary>Gets the localized search hint and tooltip title.</summary>
    public string SearchText => Localized("search_text");

    /// <summary>Gets the localized search tooltip description.</summary>
    public string SearchDescription => Localized("search_description");

    /// <summary>Gets the localized entry-count label of the toolbar.</summary>
    public string EntriesText => Localized("entries_text");

    /// <summary>Gets the localized refresh button text.</summary>
    public string RefreshText => Localized("refresh_text");

    /// <summary>Gets the localized refresh tooltip description.</summary>
    public string RefreshDescription => Localized("refresh_description");

    /// <summary>Gets the localized clear-all button text.</summary>
    public string ClearAllText => Localized("clearAll_text");

    /// <summary>Gets the localized clear-all tooltip description.</summary>
    public string ClearAllDescription => Localized("clearAll_description");

    /// <summary>Gets the localized documentation button text.</summary>
    public string OpenHelpText => Localized("openHelp_text");

    /// <summary>Gets the localized documentation tooltip description.</summary>
    public string OpenHelpDescription => Localized("openHelp_description");

    /// <summary>Gets the localized code column header.</summary>
    public string CodeHeader => Localized("code_header");

    /// <summary>Gets the localized scope column header.</summary>
    public string ScopeHeader => Localized("scope_header");

    /// <summary>Gets the localized identifier column header.</summary>
    public string IdHeader => Localized("id_header");

    /// <summary>Gets the localized target column header.</summary>
    public string TargetHeader => Localized("target_header");

    /// <summary>Gets the localized show-entry button tooltip.</summary>
    public string ShowSelectedText => Localized("showSelected_text");

    /// <summary>Gets the localized remove-entry button tooltip.</summary>
    public string RemoveSelectedText => Localized("removeSelected_text");

    /// <summary>Gets the command that initializes the pane when the view is loaded.</summary>
    public ICommand InitializeCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that opens the pane documentation.</summary>
    public ICommand OpenHelpCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that reloads the ignore list.</summary>
    public ICommand RefreshCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that removes every ignore entry.</summary>
    public ICommand ClearAllCommand { get; } = new RelayCommand(() => { });

    /// <summary>Gets the command that shows a row target in the model.</summary>
    public ICommand ShowSelectedCommand { get; } = new RelayCommand<object?>(_ => { });

    /// <summary>Gets the command that stops ignoring a row target.</summary>
    public ICommand RemoveSelectedCommand { get; } = new RelayCommand<object?>(_ => { });

    private static string Localized(string key) => LocalizationResourceReader.GetString(ResourceBaseName, key);
}
