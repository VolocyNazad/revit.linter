using Revit.Linter.Presentation.ViewModels;
using System.Globalization;

namespace Revit.Linter.Presentation.Tests;

/// <summary>
/// Verifies that the age of a report row is measured against the moment it is rendered, which is what
/// lets an open report pane move the age forward instead of freezing it.
/// </summary>
public sealed class ReportRowAgeTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
    private static readonly DateTime Now = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Local);

    [Fact]
    public void Format_measures_the_row_against_the_given_moment()
    {
        DateTime created = Now.AddMinutes(-2);

        Assert.Equal("2 minutes ago", ReportRowAge.Format(created, Now, English));
        Assert.Equal("3 hours ago", ReportRowAge.Format(created, Now.AddHours(3), English));
    }

    [Fact]
    public void Format_states_a_row_that_just_appeared_as_now()
    {
        Assert.Equal("now", ReportRowAge.Format(Now, Now, English));
    }
}
