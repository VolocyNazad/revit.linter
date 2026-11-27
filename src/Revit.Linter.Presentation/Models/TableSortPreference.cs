namespace Revit.Linter.Presentation.Models;

/// <summary>
/// Stores the column and direction a table is sorted by.
/// </summary>
public sealed class TableSortPreference
{
    /// <summary>Gets or sets the table identifier, one of <see cref="TableColumnIds"/>.</summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>Gets or sets the column key, one of <see cref="TableColumnKeys"/>.</summary>
    public string Column { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the sort runs from the largest value to the smallest.</summary>
    public bool Descending { get; set; }
}
