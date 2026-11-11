using Revit.Linter.Presentation.Models;
using Revit.Linter.Presentation.Services;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Presentation.Tests;

/// <summary>
/// Covers the column visibility a pane remembers: hiding a column stores only that column, showing every
/// column forgets the choice of one table, and the guided reveal returns the column afterwards.
/// </summary>
public sealed class TableColumnsServiceTests
{
    [Fact]
    public void Hiding_a_column_stores_only_that_column()
    {
        TestStore store = new();
        using TableColumnsService service = new(store);

        service.SetVisibility(TableColumnIds.Diagnostics, TableColumnKeys.Code, isVisible: false);

        Assert.False(service.IsVisible(TableColumnIds.Diagnostics, TableColumnKeys.Code));
        Assert.True(service.IsVisible(TableColumnIds.Diagnostics, TableColumnKeys.Severity));
        Assert.Equal(new[] { "diagnostics/code" }, store.CurrentValue.Hidden);
    }

    [Fact]
    public void Showing_a_column_forgets_its_stored_key()
    {
        TestStore store = new();
        using TableColumnsService service = new(store);
        service.SetVisibility(TableColumnIds.Diagnostics, TableColumnKeys.Code, isVisible: false);

        service.SetVisibility(TableColumnIds.Diagnostics, TableColumnKeys.Code, isVisible: true);

        Assert.True(service.IsVisible(TableColumnIds.Diagnostics, TableColumnKeys.Code));
        Assert.Empty(store.CurrentValue.Hidden);
    }

    [Fact]
    public void ShowAll_forgets_only_the_columns_of_its_own_table()
    {
        TestStore store = new();
        using TableColumnsService service = new(store);
        service.SetVisibility(TableColumnIds.Diagnostics, TableColumnKeys.Code, isVisible: false);
        service.SetVisibility(TableColumnIds.FixReport, TableColumnKeys.Created, isVisible: false);

        service.ShowAll(TableColumnIds.Diagnostics);

        Assert.True(service.IsVisible(TableColumnIds.Diagnostics, TableColumnKeys.Code));
        Assert.False(service.IsVisible(TableColumnIds.FixReport, TableColumnKeys.Created));
        Assert.Equal(new[] { "fix-report/created" }, store.CurrentValue.Hidden);
    }

    [Fact]
    public void A_saved_hidden_column_stays_hidden_across_a_new_service()
    {
        TestStore store = new()
        {
            CurrentValue = new TableColumnsSettings { Hidden = [$"{TableColumnIds.IgnoreList}/{TableColumnKeys.Scope}"] },
        };

        using TableColumnsService service = new(store);

        Assert.False(service.IsVisible(TableColumnIds.IgnoreList, TableColumnKeys.Scope));
    }

    [Fact]
    public void Reveal_shows_a_hidden_column_without_storing_it_and_restores_it_afterwards()
    {
        TestStore store = new();
        using TableColumnsService service = new(store);
        service.SetVisibility(TableColumnIds.Diagnostics, TableColumnKeys.IsActive, isVisible: false);

        using (service.Reveal(TableColumnIds.Diagnostics, TableColumnKeys.IsActive))
        {
            Assert.True(service.IsVisible(TableColumnIds.Diagnostics, TableColumnKeys.IsActive));
            Assert.Equal(["diagnostics/is-active"], store.CurrentValue.Hidden);
        }

        Assert.False(service.IsVisible(TableColumnIds.Diagnostics, TableColumnKeys.IsActive));
    }

    [Fact]
    public void Reveal_of_a_visible_column_changes_nothing_when_it_ends()
    {
        TestStore store = new();
        using TableColumnsService service = new(store);

        using (service.Reveal(TableColumnIds.Diagnostics, TableColumnKeys.IsActive))
        {
            Assert.True(service.IsVisible(TableColumnIds.Diagnostics, TableColumnKeys.IsActive));
        }

        Assert.True(service.IsVisible(TableColumnIds.Diagnostics, TableColumnKeys.IsActive));
        Assert.Empty(store.CurrentValue.Hidden);
    }

    [Fact]
    public void A_change_reports_its_table()
    {
        TestStore store = new();
        using TableColumnsService service = new(store);
        List<string?> reported = [];
        service.Changed += (_, args) => reported.Add(args.TableId);

        service.SetVisibility(TableColumnIds.Diagnostics, TableColumnKeys.Code, isVisible: false);

        Assert.Equal(new[] { TableColumnIds.Diagnostics }, reported);
    }

    private sealed class TestStore : IValueStore<TableColumnsSettings>
    {
        private readonly List<Action<TableColumnsSettings>> _listeners = [];

        public TableColumnsSettings CurrentValue { get; set; } = new();

        public IDisposable OnChange(Action<TableColumnsSettings> listener)
        {
            _listeners.Add(listener);
            return new Subscription(_listeners, listener);
        }

        public void Update(Action<TableColumnsSettings> change)
        {
            change(CurrentValue);
            foreach (Action<TableColumnsSettings> listener in _listeners) listener(CurrentValue);
        }

        private sealed class Subscription(List<Action<TableColumnsSettings>> listeners, Action<TableColumnsSettings> listener)
            : IDisposable
        {
            public void Dispose() => listeners.Remove(listener);
        }
    }
}

