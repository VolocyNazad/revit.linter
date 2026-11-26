namespace Revit.Linter.Presentation.Models;

/// <summary>
/// Stores the text of a table search box and its two switches.
/// </summary>
public sealed class TableSearchPreference
{
    /// <summary>Gets or sets the table identifier, one of <see cref="TableColumnIds"/>.</summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>Gets or sets the text of the search box.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the search matches the case of the text.</summary>
    public bool MatchCase { get; set; }

    /// <summary>Gets or sets whether the search matches whole words only.</summary>
    public bool MatchWholeWord { get; set; }
}
