using System.ComponentModel;
using Revit.Linter.Presentation.Models;

namespace Revit.Linter.Presentation.Abstractions.Services;

/// <summary>
/// Remembers the search, the filters and the sort order of the linter tables between Revit sessions.
/// </summary>
/// <remarks>
/// The service stores one view state per table. A table without a saved state keeps its default view,
/// which is an empty search, every filter enabled and the default sort order.
/// </remarks>
public interface ITableStateService
{
    /// <summary>Gets the saved sort order of a table.</summary>
    /// <param name="tableId">The table identifier, one of <see cref="TableColumnIds"/>.</param>
    /// <returns>The saved sort order, or <see langword="null"/> when the table keeps its default order.</returns>
    TableSortPreference? GetSort(string tableId);

    /// <summary>Stores the column and direction a table is sorted by.</summary>
    /// <param name="tableId">The table identifier, one of <see cref="TableColumnIds"/>.</param>
    /// <param name="columnKey">The column key, one of <see cref="TableColumnKeys"/>.</param>
    /// <param name="direction">The direction the column is sorted by.</param>
    void SetSort(string tableId, string columnKey, ListSortDirection direction);

    /// <summary>Gets the saved search of a table.</summary>
    /// <param name="tableId">The table identifier, one of <see cref="TableColumnIds"/>.</param>
    /// <returns>The saved search, or <see langword="null"/> when the search box is empty.</returns>
    TableSearchPreference? GetSearch(string tableId);

    /// <summary>Stores the search text and the two search switches of a table.</summary>
    /// <param name="tableId">The table identifier, one of <see cref="TableColumnIds"/>.</param>
    /// <param name="text">The text of the search box.</param>
    /// <param name="matchCase">Whether the search matches the case of the text.</param>
    /// <param name="matchWholeWord">Whether the search matches whole words only.</param>
    void SetSearch(string tableId, string text, bool matchCase, bool matchWholeWord);

    /// <summary>Checks whether a filter is enabled.</summary>
    /// <param name="tableId">The table identifier, one of <see cref="TableColumnIds"/>.</param>
    /// <param name="filterGroup">The filter group, one of <see cref="TableFilterGroups"/>.</param>
    /// <param name="filterKey">The key of the filter inside its group.</param>
    /// <returns><see langword="true"/> when the filter was never disabled.</returns>
    bool IsFilterActive(string tableId, string filterGroup, string filterKey);

    /// <summary>Enables or disables a filter and stores the choice for the user.</summary>
    /// <param name="tableId">The table identifier, one of <see cref="TableColumnIds"/>.</param>
    /// <param name="filterGroup">The filter group, one of <see cref="TableFilterGroups"/>.</param>
    /// <param name="filterKey">The key of the filter inside its group.</param>
    /// <param name="isActive">Whether the filter should let its rows through.</param>
    void SetFilterActive(string tableId, string filterGroup, string filterKey, bool isActive);
}
