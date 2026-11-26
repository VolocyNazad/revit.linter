using System.ComponentModel;
using Revit.Linter.Presentation.Models;
using Revit.Linter.Presentation.Services;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Presentation.Tests;

/// <summary>
/// Covers the view state a pane remembers between Revit sessions: one sort and one search per table, and
/// the filters the user disabled. A table without an entry keeps its default view.
/// </summary>
public sealed class TableStateServiceTests
{
    [Fact]
    public void A_table_without_a_saved_state_keeps_its_defaults()
    {
        TableStateService service = new(new TestStore());

        Assert.Null(service.GetSort(TableColumnIds.Diagnostics));
        Assert.Null(service.GetSearch(TableColumnIds.Diagnostics));
        Assert.True(service.IsFilterActive(
            TableColumnIds.Diagnostics, TableFilterGroups.TargetType, "Element"));
    }

    [Fact]
    public void A_sort_is_stored_with_its_column_and_direction()
    {
        TestStore store = new();
        TableStateService service = new(store);

        service.SetSort(TableColumnIds.DiagnosticReport, TableColumnKeys.Message, ListSortDirection.Descending);

        TableSortPreference saved = Assert.IsType<TableSortPreference>(
            service.GetSort(TableColumnIds.DiagnosticReport));
        Assert.Equal(TableColumnIds.DiagnosticReport, saved.Table);
        Assert.Equal(TableColumnKeys.Message, saved.Column);
        Assert.True(saved.Descending);
    }

    [Fact]
    public void A_second_sort_replaces_the_sort_of_the_same_table_and_keeps_another_table_apart()
    {
        TestStore store = new();
        TableStateService service = new(store);
        service.SetSort(TableColumnIds.Diagnostics, TableColumnKeys.Code, ListSortDirection.Ascending);
        service.SetSort(TableColumnIds.FixReport, TableColumnKeys.Created, ListSortDirection.Descending);

        service.SetSort(TableColumnIds.Diagnostics, TableColumnKeys.Severity, ListSortDirection.Descending);

        Assert.Equal(TableColumnKeys.Severity, service.GetSort(TableColumnIds.Diagnostics)!.Column);
        Assert.Equal(TableColumnKeys.Created, service.GetSort(TableColumnIds.FixReport)!.Column);
        Assert.Equal(2, store.CurrentValue.Sorts.Count);
    }

    [Fact]
    public void A_search_is_stored_with_its_switches_and_an_empty_default_search_forgets_it()
    {
        TestStore store = new();
        TableStateService service = new(store);

        service.SetSearch(TableColumnIds.IgnoreList, "wall", matchCase: true, matchWholeWord: true);

        TableSearchPreference saved = Assert.IsType<TableSearchPreference>(
            service.GetSearch(TableColumnIds.IgnoreList));
        Assert.Equal("wall", saved.Text);
        Assert.True(saved.MatchCase);
        Assert.True(saved.MatchWholeWord);

        service.SetSearch(TableColumnIds.IgnoreList, string.Empty, matchCase: false, matchWholeWord: false);

        Assert.Null(service.GetSearch(TableColumnIds.IgnoreList));
        Assert.Empty(store.CurrentValue.Searches);
    }

    [Fact]
    public void A_disabled_filter_is_remembered_per_table_and_group()
    {
        TestStore store = new();
        TableStateService service = new(store);

        service.SetFilterActive(
            TableColumnIds.DiagnosticReport, TableFilterGroups.Severity, "Warning", isActive: false);

        Assert.False(service.IsFilterActive(
            TableColumnIds.DiagnosticReport, TableFilterGroups.Severity, "Warning"));
        Assert.True(service.IsFilterActive(
            TableColumnIds.DiagnosticReport, TableFilterGroups.Severity, "Error"));
        Assert.True(service.IsFilterActive(
            TableColumnIds.DiagnosticReport, TableFilterGroups.Flags, "Warning"));
        Assert.Equal(["diagnostic-report/severity/Warning"], store.CurrentValue.InactiveFilters);
    }

    [Fact]
    public void Enabling_a_filter_forgets_its_stored_key()
    {
        TestStore store = new();
        TableStateService service = new(store);
        service.SetFilterActive(
            TableColumnIds.Diagnostics, TableFilterGroups.TargetType, "Document", isActive: false);

        service.SetFilterActive(
            TableColumnIds.Diagnostics, TableFilterGroups.TargetType, "Document", isActive: true);

        Assert.True(service.IsFilterActive(
            TableColumnIds.Diagnostics, TableFilterGroups.TargetType, "Document"));
        Assert.Empty(store.CurrentValue.InactiveFilters);
    }

    private sealed class TestStore : IValueStore<TableStateSettings>
    {
        public TableStateSettings CurrentValue { get; set; } = new();

        public IDisposable OnChange(Action<TableStateSettings> listener) => new Subscription();

        public void Update(Action<TableStateSettings> change) => change(CurrentValue);

        private sealed class Subscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
