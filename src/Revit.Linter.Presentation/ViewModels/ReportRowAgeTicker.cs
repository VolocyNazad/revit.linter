using System.Windows.Threading;

namespace Revit.Linter.Presentation.ViewModels;

/// <summary>
/// Refreshes the relative age shown for the rows of an open report.
/// </summary>
/// <remarks>
/// A row computes its age against the current time when the binding evaluates, so a cell keeps the text
/// it got when it appeared. A report pane stays open far longer than the rows change, and the age would
/// otherwise freeze. One ticker per pane re-raises the property on a fixed interval; WPF re-evaluates it
/// only for the cells the list has realized, so the cost does not grow with the size of the report.
/// </remarks>
public sealed class ReportRowAgeTicker : IDisposable
{
    /// <summary>Gets the interval between two refreshes of the row ages.</summary>
    /// <remarks>
    /// The relative text changes at minute boundaries, so a half-minute interval keeps it within one
    /// step of the current time without waking the UI thread more often than needed.
    /// </remarks>
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(30);

    private readonly Action _refresh;
    private readonly DispatcherTimer _timer;
    private bool _disposed;

    /// <summary>Starts refreshing the row ages.</summary>
    /// <param name="refresh">Re-raises the relative age of every row of the report.</param>
    /// <param name="dispatcher">The dispatcher the timer runs on, normally the UI thread.</param>
    /// <param name="interval">The refresh interval; <see cref="DefaultInterval"/> when omitted.</param>
    public ReportRowAgeTicker(Action refresh, Dispatcher dispatcher, TimeSpan? interval = null)
    {
        _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));
        if (dispatcher is null) throw new ArgumentNullException(nameof(dispatcher));

        _timer = new DispatcherTimer(
            interval ?? DefaultInterval, DispatcherPriority.Background, OnTick, dispatcher);
        _timer.Start();
    }

    /// <summary>Re-raises the relative age of every row of the report once.</summary>
    public void Refresh() => _refresh();

    /// <summary>Stops refreshing the row ages.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
    }

    private void OnTick(object? sender, EventArgs e) => Refresh();
}
