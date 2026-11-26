namespace Revit.Linter.Presentation.Models;

/// <summary>
/// Names the filter groups whose state a table remembers.
/// </summary>
/// <remarks>
/// A group keeps the filters of one popup independent of another popup of the same table. The name is
/// written to <c>tables.yml</c> together with the filter key.
/// </remarks>
public static class TableFilterGroups
{
    /// <summary>The element and document filters of the diagnostics table.</summary>
    public const string TargetType = "target-type";

    /// <summary>The message, warning and error filters of the diagnostic report table.</summary>
    public const string Severity = "severity";

    /// <summary>The actual and obsolete filters of the diagnostic report table.</summary>
    public const string Flags = "flags";
}
