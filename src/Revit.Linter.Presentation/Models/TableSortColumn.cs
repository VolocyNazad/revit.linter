namespace Revit.Linter.Presentation.Models;

/// <summary>
/// Maps a saved column key to the property the table sorts that column by.
/// </summary>
/// <param name="ColumnKey">The column key, one of <see cref="TableColumnKeys"/>.</param>
/// <param name="MemberPath">The property name the view sorts by, the one the view sets as its sort member path.</param>
public sealed record TableSortColumn(string ColumnKey, string MemberPath);
