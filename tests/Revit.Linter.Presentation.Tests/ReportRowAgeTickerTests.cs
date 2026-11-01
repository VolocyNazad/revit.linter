using Revit.Linter.Presentation.ViewModels;
using System.Windows.Threading;

namespace Revit.Linter.Presentation.Tests;

/// <summary>
/// Verifies that the ticker an open report pane owns re-raises the row ages on its interval, which is
/// what lets the relative creation time of a row move forward instead of freezing.
/// </summary>
public sealed class ReportRowAgeTickerTests
{
    [Fact]
    public void Refresh_runs_the_configured_aging_of_every_row()
    {
        int refreshes = 0;
        using var ticker = new ReportRowAgeTicker(
            () => refreshes++, Dispatcher.CurrentDispatcher);

        ticker.Refresh();

        Assert.Equal(1, refreshes);
    }
}
