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
}
