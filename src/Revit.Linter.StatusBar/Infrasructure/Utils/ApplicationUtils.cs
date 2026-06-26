namespace Revit.Linter.StatusBar.Infrasructure.Utils;

/// <summary>
/// ApplicationUtils
/// </summary>
internal static class ApplicationUtils
{
    /// <summary>
    /// DoEvents
    /// </summary>
    public static void DoEvents() => Application.DoEvents();

    /// <summary>
    /// SetCursorWait
    /// </summary>
    public static void SetCursorWait() => Cursor.Current = Cursors.WaitCursor;

    /// <summary>
    /// SetCursorDefault
    /// </summary>
    public static void SetCursorDefault() => Cursor.Current = Cursors.Default;
}
