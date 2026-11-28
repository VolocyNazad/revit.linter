namespace Revit.Linter.Presentation.Models;

/// <summary>
/// Stores the width the user dragged a table column to.
/// </summary>
public sealed class TableColumnWidth
{
    /// <summary>Gets or sets the table identifier, one of <see cref="TableColumnIds"/>.</summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>Gets or sets the column key, one of <see cref="TableColumnKeys"/>.</summary>
    public string Column { get; set; } = string.Empty;

    /// <summary>Gets or sets the column width in device-independent pixels.</summary>
    public double Pixels { get; set; }
}
