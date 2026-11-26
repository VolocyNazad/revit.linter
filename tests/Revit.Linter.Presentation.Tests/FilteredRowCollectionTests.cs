using System.Collections.ObjectModel;
using System.ComponentModel;
using Revit.Linter.Presentation.Models;
using Revit.Linter.Presentation.Services;
using Revit.Linter.Presentation.ViewModels;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Presentation.Tests;

/// <summary>
/// Covers the view state a guided session resets: the default sort order a column click replaces must be
/// restorable, so a session can bring its own row back to the top. Also covers the code order itself,
/// which reads the number of a code as a number, and the saved sort order of a table.
/// </summary>
public sealed class FilteredRowCollectionTests
{
    [Fact]
    public void A_column_sorted_by_the_code_key_orders_numbers_as_numbers()
    {
        ObservableCollection<Row> source = new() { new("CSTM10"), new("CSTM2"), new("CSTM1") };
        FilteredRowCollection<Row> rows = new(
            [new SortDescription(nameof(Row.CodeSortKey), ListSortDirection.Ascending)],
            _ => true);
        rows.Attach(source);

        Assert.Equal(new[] { "CSTM1", "CSTM2", "CSTM10" }, Codes(rows));
    }

    [Fact]
    public void ResetSort_restores_the_default_order_after_a_column_was_sorted()
    {
        ObservableCollection<Row> source = new() { new("b"), new("a") };
        FilteredRowCollection<Row> rows = new(
            [new SortDescription(nameof(Row.Code), ListSortDirection.Ascending)],
            _ => true);
        rows.Attach(source);

        rows.ViewSource!.SortDescriptions.Clear();
        rows.ViewSource.SortDescriptions.Add(new SortDescription(nameof(Row.Code), ListSortDirection.Descending));
        rows.Refresh();

        Assert.Equal(new[] { "b", "a" }, Codes(rows));

        rows.ResetSort();

        Assert.Equal(new[] { "a", "b" }, Codes(rows));
    }

    [Fact]
    public void ResetSort_applies_every_default_sort_description_in_order()
    {
        ObservableCollection<Row> source = new()
        {
            new("group-b", "b"),
            new("group-a", "b"),
            new("group-a", "a"),
        };
        FilteredRowCollection<Row> rows = new(
            [
                new SortDescription(nameof(Row.Group), ListSortDirection.Ascending),
                new SortDescription(nameof(Row.Code), ListSortDirection.Ascending),
            ],
            _ => true);
        rows.Attach(source);

        rows.ViewSource!.SortDescriptions.Clear();
        rows.ViewSource.SortDescriptions.Add(new SortDescription(nameof(Row.Code), ListSortDirection.Descending));
        rows.Refresh();

        rows.ResetSort();

        Assert.Equal(
            new[] { "group-a:a", "group-a:b", "group-b:b" },
            rows.ViewSource.View.Cast<Row>().Select(row => $"{row.Group}:{row.Code}").ToArray());
    }

    [Fact]
    public void ResetSort_before_the_collection_is_attached_does_nothing()
    {
        FilteredRowCollection<Row> rows = new(
            [new SortDescription(nameof(Row.Code), ListSortDirection.Ascending)],
            _ => true);

        Assert.Null(rows.ViewSource);
        rows.ResetSort();
        Assert.Null(rows.ViewSource);
    }

    [Fact]
    public void Attach_restores_the_saved_sort_of_the_table()
    {
        TestStore store = new()
        {
            CurrentValue = new TableStateSettings
            {
                Sorts =
                [
                    new TableSortPreference
                    {
                        Table = TableColumnIds.Diagnostics,
                        Column = TableColumnKeys.Code,
                        Descending = true,
                    },
                ],
            },
        };
        TableStateService service = new(store);
        ObservableCollection<Row> source = new() { new("a"), new("b") };
        using FilteredRowCollection<Row> rows = CreateRows(service);
        rows.Attach(source);

        Assert.Equal(new[] { "b", "a" }, Codes(rows));
        SortDescription sort = rows.ViewSource!.View.SortDescriptions[0];
        Assert.Equal(nameof(Row.CodeSortKey), sort.PropertyName);
        Assert.Equal(ListSortDirection.Descending, sort.Direction);
    }

    [Fact]
    public void A_sort_applied_by_the_user_is_saved()
    {
        TestStore store = new();
        TableStateService service = new(store);
        using FilteredRowCollection<Row> rows = CreateRows(service);
        rows.Attach(new ObservableCollection<Row> { new("a"), new("b") });

        SortDescriptionCollection sorts = rows.ViewSource!.View.SortDescriptions;
        sorts.Clear();
        sorts.Add(new SortDescription(nameof(Row.CodeSortKey), ListSortDirection.Descending));

        TableSortPreference saved = Assert.IsType<TableSortPreference>(
            service.GetSort(TableColumnIds.Diagnostics));
        Assert.Equal(TableColumnKeys.Code, saved.Column);
        Assert.True(saved.Descending);
    }

    [Fact]
    public void ResetSort_does_not_replace_the_sort_the_user_saved()
    {
        TestStore store = new()
        {
            CurrentValue = new TableStateSettings
            {
                Sorts =
                [
                    new TableSortPreference
                    {
                        Table = TableColumnIds.Diagnostics,
                        Column = TableColumnKeys.Code,
                        Descending = true,
                    },
                ],
            },
        };
        TableStateService service = new(store);
        using FilteredRowCollection<Row> rows = CreateRows(service);
        rows.Attach(new ObservableCollection<Row> { new("a"), new("b") });

        rows.ResetSort();

        Assert.Equal(new[] { "a", "b" }, Codes(rows));
        TableSortPreference saved = Assert.IsType<TableSortPreference>(
            service.GetSort(TableColumnIds.Diagnostics));
        Assert.True(saved.Descending);
    }

    [Fact]
    public void A_saved_sort_of_a_missing_column_keeps_the_default_order()
    {
        TestStore store = new()
        {
            CurrentValue = new TableStateSettings
            {
                Sorts =
                [
                    new TableSortPreference
                    {
                        Table = TableColumnIds.Diagnostics,
                        Column = TableColumnKeys.Severity,
                        Descending = true,
                    },
                ],
            },
        };
        TableStateService service = new(store);
        using FilteredRowCollection<Row> rows = CreateRows(service);
        rows.Attach(new ObservableCollection<Row> { new("b"), new("a") });

        Assert.Equal(new[] { "a", "b" }, Codes(rows));
    }

    private static FilteredRowCollection<Row> CreateRows(TableStateService service) =>
        new(
            [new SortDescription(nameof(Row.CodeSortKey), ListSortDirection.Ascending)],
            _ => true,
            tableId: TableColumnIds.Diagnostics,
            sortColumns: [new TableSortColumn(TableColumnKeys.Code, nameof(Row.CodeSortKey))],
            tableState: service);

    private static string[] Codes(FilteredRowCollection<Row> rows) =>
        rows.ViewSource!.View.Cast<Row>().Select(row => row.Code).ToArray();

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

    private sealed class Row(string group, string code)
    {
        public Row(string code)
            : this("group", code)
        {
        }

        public string Group { get; } = group;
        public string Code { get; } = code;
        public DiagnosticCodeSortKey CodeSortKey => new(Code);
    }
}
