namespace Revit.Linter.ReportMessaging;

/// <summary>
/// Limits how many items of a list-valued message placeholder a report table renders inline.
/// </summary>
/// <remarks>
/// A finding message may carry a list of elements: the identifiers of the failing elements or the names
/// of everything an element intersects. Rendering the whole list stretches the row of the table, so the
/// list is folded into its first items and an expander that reveals the complete list. Scalar values,
/// including long prose such as a Revit warning description, are never folded: shortening them would
/// hide data that has no list structure to expand.
/// </remarks>
public sealed record ReportDisplayBudget
{
    /// <summary>
    /// Gets the budget the report tables use.
    /// </summary>
    public static ReportDisplayBudget Default { get; } = new();

    /// <summary>
    /// Gets the number of list items rendered before the remaining items are folded into one expander.
    /// </summary>
    public int MaxInlineListItems { get; init; } = 3;

    /// <summary>
    /// Gets the character that opens a name in an inline name list.
    /// </summary>
    /// <remarks>
    /// A name is free text and may contain the item separator, so every name is wrapped in typographic
    /// quotes: the reader sees where one name ends and the next begins. The quotes are punctuation and
    /// the same in every culture; an identifier list is not quoted, because a digit run cannot be
    /// mistaken for the separator.
    /// </remarks>
    public string ItemOpenQuote { get; init; } = "\u201C";

    /// <summary>
    /// Gets the character that closes a name in an inline name list.
    /// </summary>
    public string ItemCloseQuote { get; init; } = "\u201D";
}
