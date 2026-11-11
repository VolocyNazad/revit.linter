using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.Presentation.Models;

/// <summary>
/// Stores the table columns the user hid.
/// </summary>
/// <remarks>
/// One file serves every Revit version. Only hidden columns are stored: an absent key means the column is
/// visible, so a column added by a later release appears without a migration. Each key is written as
/// <c>table/column</c>.
/// </remarks>
[StoreFile("columns.yml")]
public sealed class TableColumnsSettings
{
    /// <summary>Gets or sets the hidden columns, each written as <c>table/column</c>.</summary>
    public List<string> Hidden { get; set; } = [];
}
