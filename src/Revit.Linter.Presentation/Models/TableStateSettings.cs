using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Presentation.Models;

/// <summary>
/// Stores the search, the filters and the sort order the user left in the linter tables.
/// </summary>
/// <remarks>
/// One file serves every Revit version, like <c>columns.yml</c>. A table without an entry uses its default
/// view, so a table added by a later release needs no migration.
/// </remarks>
[StoreFile("tables.yml")]
public sealed class TableStateSettings
{
    /// <summary>Gets or sets the saved sort order, at most one entry per table.</summary>
    public List<TableSortPreference> Sorts { get; set; } = [];

    /// <summary>Gets or sets the saved search text and search switches, at most one entry per table.</summary>
    public List<TableSearchPreference> Searches { get; set; } = [];

    /// <summary>Gets or sets the disabled filters, each written as <c>table/filter-group/filter-key</c>.</summary>
    public List<string> InactiveFilters { get; set; } = [];
}
