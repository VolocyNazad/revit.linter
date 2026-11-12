using Revit.Linter.Presentation.Abstractions.Models;
using Revit.Linter.Presentation.Models;

namespace Revit.Linter.Presentation.Abstractions.Services;

/// <summary>
/// Remembers which data columns of the linter tables are visible and notifies the open panes about a change.
/// </summary>
public interface ITableColumnsService
{
    /// <summary>Raised when a saved or temporary column visibility change affects one or more tables.</summary>
    event EventHandler<TableColumnsChangedEventArgs>? Changed;

    /// <summary>Checks whether a column is currently visible.</summary>
    /// <param name="tableId">The table identifier, one of <see cref="TableColumnIds"/>.</param>
    /// <param name="columnKey">The column key, one of <see cref="TableColumnKeys"/>.</param>
    /// <returns><see langword="true"/> when the column is visible or was never hidden.</returns>
    bool IsVisible(string tableId, string columnKey);

    /// <summary>Hides or shows a column and stores the choice for the user.</summary>
    /// <param name="tableId">The table identifier, one of <see cref="TableColumnIds"/>.</param>
    /// <param name="columnKey">The column key, one of <see cref="TableColumnKeys"/>.</param>
    /// <param name="isVisible">Whether the column should be visible.</param>
    void SetVisibility(string tableId, string columnKey, bool isVisible);

    /// <summary>Shows every column of a table and stores that choice for the user.</summary>
    /// <param name="tableId">The table identifier, one of <see cref="TableColumnIds"/>.</param>
    void ShowAll(string tableId);

    /// <summary>Shows a column for the duration of a guided action and restores its previous visibility.</summary>
    /// <param name="tableId">The table identifier, one of <see cref="TableColumnIds"/>.</param>
    /// <param name="columnKey">The column key, one of <see cref="TableColumnKeys"/>.</param>
    /// <returns>A scope that restores the visibility saved when the reveal started.</returns>
    /// <remarks>
    /// The reveal is temporary: it never writes <c>columns.yml</c> and disposing the scope returns the
    /// column to the state the user chose.
    /// </remarks>
    IDisposable Reveal(string tableId, string columnKey);
}
