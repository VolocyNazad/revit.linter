using Revit.Linter.Presentation.Models;
using Revit.Linter.Presentation.Services;
using Revit.Linter.Presentation.ViewModels;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Presentation.Tests;

/// <summary>
/// Covers the popup of a table: it keeps one data column visible, follows a visibility change made
/// elsewhere and shows every column again on request.
/// </summary>
public sealed class TableColumnsViewModelTests
{
    [Fact]
    public void The_last_visible_column_cannot_be_hidden()
    {
        using TableColumnsService service = new(new TestStore());
        using TableColumnsViewModel viewModel = new(
            TableColumnIds.Diagnostics,
            service,
            [new(TableColumnKeys.Code, () => "Code"), new(TableColumnKeys.Tags, () => "Tags")]);
        viewModel.Columns[1].IsVisible = false;

        viewModel.Columns[0].IsVisible = false;

        Assert.True(viewModel.Columns[0].IsVisible);
        Assert.True(service.IsVisible(TableColumnIds.Diagnostics, TableColumnKeys.Code));
        Assert.False(service.IsVisible(TableColumnIds.Diagnostics, TableColumnKeys.Tags));
    }

    [Fact]
    public void Hiding_a_column_is_published_to_the_table_and_the_popup()
    {
        using TableColumnsService service = new(new TestStore());
        using TableColumnsViewModel viewModel = new(
            TableColumnIds.Diagnostics,
            service,
            [new(TableColumnKeys.Code, () => "Code"), new(TableColumnKeys.Tags, () => "Tags")]);
        int published = 0;
        viewModel.VisibilityChanged += (_, _) => published++;

        viewModel.Columns[1].IsVisible = false;

        Assert.False(service.IsVisible(TableColumnIds.Diagnostics, TableColumnKeys.Tags));
        Assert.False(viewModel.Columns[1].IsVisible);
        Assert.True(published > 0);
    }

    [Fact]
    public void A_change_made_elsewhere_updates_the_popup()
    {
        using TableColumnsService service = new(new TestStore());
        using TableColumnsViewModel viewModel = new(
            TableColumnIds.Diagnostics,
            service,
            [new(TableColumnKeys.Code, () => "Code"), new(TableColumnKeys.Tags, () => "Tags")]);

        service.SetVisibility(TableColumnIds.Diagnostics, TableColumnKeys.Tags, isVisible: false);

        Assert.False(viewModel.Columns[1].IsVisible);
    }

    [Fact]
    public void ShowAll_shows_every_column_again()
    {
        using TableColumnsService service = new(new TestStore());
        using TableColumnsViewModel viewModel = new(
            TableColumnIds.Diagnostics,
            service,
            [new(TableColumnKeys.Code, () => "Code"), new(TableColumnKeys.Tags, () => "Tags")]);
        viewModel.Columns[1].IsVisible = false;

        viewModel.ShowAllCommand.Execute(null);

        Assert.All(viewModel.Columns, column => Assert.True(column.IsVisible));
    }

    [Fact]
    public void A_table_whose_saved_columns_are_all_hidden_shows_every_column()
    {
        TestStore store = new()
        {
            CurrentValue = new TableColumnsSettings
            {
                Hidden =
                [
                    $"{TableColumnIds.FixReport}/{TableColumnKeys.Code}",
                    $"{TableColumnIds.FixReport}/{TableColumnKeys.Message}",
                ],
            },
        };
        using TableColumnsService service = new(store);

        using TableColumnsViewModel viewModel = new(
            TableColumnIds.FixReport,
            service,
            [new(TableColumnKeys.Code, () => "Code"), new(TableColumnKeys.Message, () => "Message")]);

        Assert.All(viewModel.Columns, column => Assert.True(column.IsVisible));
        Assert.Empty(store.CurrentValue.Hidden);
    }

    [Fact]
    public void A_popup_ignores_the_changes_of_another_table()
    {
        using TableColumnsService service = new(new TestStore());
        using TableColumnsViewModel viewModel = new(
            TableColumnIds.Diagnostics,
            service,
            [new(TableColumnKeys.Code, () => "Code")]);

        service.SetVisibility(TableColumnIds.IgnoreList, TableColumnKeys.Code, isVisible: false);

        Assert.True(viewModel.Columns[0].IsVisible);
    }

    [Fact]
    public void The_title_follows_the_current_culture()
    {
        using TableColumnsService service = new(new TestStore());
        string title = "Code";
        using TableColumnsViewModel viewModel = new(
            TableColumnIds.Diagnostics,
            service,
            [new(TableColumnKeys.Code, () => title)]);

        title = "Код";

        Assert.Equal("Код", viewModel.Columns[0].Title);
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
