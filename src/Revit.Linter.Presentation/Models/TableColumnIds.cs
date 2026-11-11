namespace Revit.Linter.Presentation.Models;

/// <summary>
/// Names the tables whose column visibility the add-in remembers.
/// </summary>
/// <remarks>
/// The identifier is written to <c>columns.yml</c>, so renaming one forgets the columns the user hid in that
/// table. Column keys are listed in <see cref="TableColumnKeys"/>.
/// </remarks>
public static class TableColumnIds
{
    /// <summary>The table of the diagnostics pane.</summary>
    public const string Diagnostics = "diagnostics";

    /// <summary>The table of the findings report pane.</summary>
    public const string DiagnosticReport = "diagnostic-report";

    /// <summary>The table of the fix report pane.</summary>
    public const string FixReport = "fix-report";

    /// <summary>The table of the ignore list pane.</summary>
    public const string IgnoreList = "ignore-list";
}
