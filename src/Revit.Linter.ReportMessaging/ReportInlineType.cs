namespace Revit.Linter.ReportMessaging;

/// <summary>
/// Identifies how a part of a report message is rendered.
/// </summary>
public enum ReportInlineType
{
    /// <summary>
    /// Renders the part as plain text.
    /// </summary>
    Text,

    /// <summary>
    /// Renders the part as an interactive hyperlink.
    /// </summary>
    Hyperlink,

    /// <summary>
    /// Renders the part as an interactive control that reveals a folded message list.
    /// </summary>
    Expander,

    /// <summary>
    /// Renders the part as a line break, so a revealed list shows one item per line.
    /// </summary>
    /// <remarks>
    /// The part text is the matching plain-text separator, a newline, so the plain text of the parts
    /// stays readable.
    /// </remarks>
    LineBreak,
}
