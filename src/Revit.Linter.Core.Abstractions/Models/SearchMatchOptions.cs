namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>
/// Names how the text of a search box is compared with the text of a row.
/// </summary>
/// <param name="MatchCase">Whether the row text must have the same letter case as the search text.</param>
/// <param name="MatchWholeWord">Whether the search text must start and end on a word boundary.</param>
public readonly record struct SearchMatchOptions(bool MatchCase, bool MatchWholeWord)
{
    /// <summary>Gets the default comparison: any case, a part of a word.</summary>
    public static SearchMatchOptions Default => new(false, false);
}
