using Humanizer;
using System.Globalization;

namespace Revit.Linter.Presentation.ViewModels;

/// <summary>
/// Renders how long ago a report row was created as relative text.
/// </summary>
/// <remarks>
/// The age is measured against a caller-supplied moment rather than against the clock inside the
/// formatter, so a row can be re-rendered as time passes and the rendering itself stays testable.
/// </remarks>
public static class ReportRowAge
{
    /// <summary>Gets the age of a row created at the given local time, as seen at the given local time.</summary>
    /// <param name="created">The local time the row was created.</param>
    /// <param name="now">The local time the age is measured against.</param>
    /// <param name="culture">The culture the text is written in.</param>
    /// <returns>The localized distance of time in words, for example <c>2 minutes ago</c>.</returns>
    public static string Format(DateTime created, DateTime now, CultureInfo culture)
    {
        // The row stores local wall-clock time, so the comparison base stays in the same frame and
        // Humanizer does not move it between time zones.
        return created.Humanize(utcDate: false, dateToCompareAgainst: now, culture: culture);
    }
}
