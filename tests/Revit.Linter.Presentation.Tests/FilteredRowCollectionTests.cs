using System.Collections.ObjectModel;
using System.ComponentModel;
using Revit.Linter.Presentation.Models;
using Revit.Linter.Presentation.ViewModels;

namespace Revit.Linter.Presentation.Tests;

/// <summary>
/// Covers the view state a guided session resets: the default sort order a column click replaces must be
/// restorable, so a session can bring its own row back to the top. Also covers the code order itself,
/// which reads the number of a code as a number.
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

    private static string[] Codes(FilteredRowCollection<Row> rows) =>
        rows.ViewSource!.View.Cast<Row>().Select(row => row.Code).ToArray();

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
