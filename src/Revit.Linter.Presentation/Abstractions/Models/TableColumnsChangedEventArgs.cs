namespace Revit.Linter.Presentation.Abstractions.Models;

/// <summary>Describes the table a column visibility change belongs to.</summary>
public sealed class TableColumnsChangedEventArgs(string? tableId) : EventArgs
{
    /// <summary>Gets the affected table, or <see langword="null"/> when every table may have changed.</summary>
    public string? TableId { get; } = tableId;
}
