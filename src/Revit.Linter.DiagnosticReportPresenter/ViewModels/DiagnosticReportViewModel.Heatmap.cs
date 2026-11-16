// Aggregation, application and restoration of the findings heat map.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Revit.Async;
using Revit.Linter.DialogPresenter.Abstractions;
using System.Windows.Threading;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

internal sealed partial class DiagnosticReportViewModel
{
    private IElementHeatmapSession? _activeHeatmap;
    private Document? _activeHeatmapDocument;
    private readonly object _heatmapRestoreSync = new();
    private Task? _scheduledHeatmapRestore;

    [ObservableProperty]
    private bool _isHeatmapActive;

    [RelayCommand]
    private async Task ToggleHeatmap(CancellationToken cancellationToken)
    {
        if (_activeHeatmap is not null
            && _activeHeatmapDocument is { IsValidObject: true } activeHeatmapDocument
            && activeHeatmapDocument.Equals(_revitContext.ActiveDocument))
        {
            await RemoveHeatmapAsync(cancellationToken);
            return;
        }

        Document? document = _revitContext.ActiveDocument;
        if (document is null)
        {
            IsHeatmapActive = false;
            await _dialog.Show(
                new DialogRequest(GetLocalizedString("heatmapNoDocument_message")), cancellationToken);
            return;
        }

        if (document.IsFamilyDocument)
        {
            IsHeatmapActive = false;
            await _dialog.Show(
                new DialogRequest(GetLocalizedString("heatmapFamilyDocument_message")), cancellationToken);
            return;
        }

        IReadOnlyList<FindingHeatmapEntry> entries = CreateHeatmapEntries(document);
        if (entries.Count == 0)
        {
            IsHeatmapActive = false;
            await _dialog.Show(
                new DialogRequest(GetLocalizedString("heatmapNoFindings_message")), cancellationToken);
            return;
        }

        try
        {
            (IElementHeatmapSession? session, int coloredCount, int skippedCount) =
                await RevitTask.RunAsync(_ => ApplyHeatmap(document, entries));
            if (session is null || coloredCount == 0)
            {
                session?.Dispose();
                IsHeatmapActive = false;
                await _dialog.Show(
                    new DialogRequest(GetLocalizedString("heatmapNoGeometry_message")), cancellationToken);
                return;
            }

            _activeHeatmap = session;
            _activeHeatmapDocument = document;
            IsHeatmapActive = true;
            _logger.LogInformation(
                "Applied the findings heat map for document {DocumentTitle}: {ColoredCount} elements, " +
                "{SkippedCount} skipped",
                document.Title, coloredCount, skippedCount);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to apply the findings heat map for document {DocumentTitle}",
                document.Title);
            IsHeatmapActive = false;
            await _dialog.Show(
                new DialogRequest(GetLocalizedString("heatmapFailed_message", exception.Message)),
                cancellationToken);
        }
    }

    private IReadOnlyList<FindingHeatmapEntry> CreateHeatmapEntries(Document document)
    {
        string documentTitle = document.Title;
        List<FindingHeatmapFinding<ElementId>> findings = [];
        foreach (DiagnosticReportItemViewModel item in Collection)
        {
            if (!string.Equals(item.DocumentTitle, documentTitle, StringComparison.Ordinal)) continue;
            if (item.IsObsolete) continue;
            if (item.TargetElementId is not { } elementId) continue;

            findings.Add(new FindingHeatmapFinding<ElementId>(elementId, item.Severity, item.Code));
        }

        return FindingHeatmapAggregator.Aggregate<ElementId>(findings)
            .Select(value => new FindingHeatmapEntry(value.Element, value.Severity, value.Count))
            .ToArray();
    }

    // Runs in the Revit API context: creates the persistent visualization view when it is missing,
    // applies the coloured overrides and brings the view on screen after the transaction is committed.
    private (IElementHeatmapSession? Session, int ColoredCount, int SkippedCount) ApplyHeatmap(
        Document document,
        IReadOnlyList<FindingHeatmapEntry> entries)
    {
        RestoreActiveHeatmap();

        // A finding visualization colours the same view through its own overrides; it is restored before
        // the map is applied so the two sessions do not write stale graphics over each other.
        RestoreActiveVisualization();

        View3D view = _visualizationViewProvider.GetOrCreateView();
        IElementHeatmapSession session = _heatmapService.Apply(document, view, entries);
        _visualizationViewProvider.Activate(view);
        return (session, session.ColoredElementCount, session.SkippedElementCount);
    }

    private async Task RemoveHeatmapAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RevitTask.RunAsync(_ => RestoreActiveHeatmap());
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to remove the findings heat map");
            IsHeatmapActive = true;
            await _dialog.Show(
                new DialogRequest(GetLocalizedString("heatmapRestoreFailed_message", exception.Message)),
                cancellationToken);
        }
    }

    // Runs in the Revit API context. The session keeps its graphics when the restore fails, so a later
    // attempt can still remove the heat map.
    private void RestoreActiveHeatmap()
    {
        IElementHeatmapSession? session = _activeHeatmap;
        if (session is null) return;

        _logger.LogInformation("Restoring the findings heat map");
        session.Restore();
        _activeHeatmap = null;
        _activeHeatmapDocument = null;
        session.Dispose();
        RefreshHeatmapState(false);
    }

    // The report can be cleared from a WPF handler or a configuration-change notification, where Revit
    // does not allow the transaction that restores the graphics. A failed direct restore is retried on
    // the next Idling event, the same way the finding visualization handles it.
    private void RestoreHeatmapOrSchedule()
    {
        if (_activeHeatmap is null) return;

        try
        {
            RestoreActiveHeatmap();
        }
        catch (Exception)
        {
            _ = RestoreHeatmapScheduledAsync();
        }
    }

    // A map shown in one document must not stay armed while another document is active: its view is not
    // on screen, and pressing the button would otherwise remove the map instead of building a new one.
    private void RestoreHeatmapIfDocumentChanged()
    {
        if (_activeHeatmap is null || _activeHeatmapDocument is not { } heatmapDocument) return;

        Document? activeDocument = _revitContext.ActiveDocument;
        if (!heatmapDocument.IsValidObject
            || activeDocument is null
            || !heatmapDocument.Equals(activeDocument))
            RestoreHeatmapOrSchedule();
    }

    private Task RestoreHeatmapScheduledAsync(CancellationToken cancellationToken = default)
    {
        if (_activeHeatmap is null) return Task.CompletedTask;

        lock (_heatmapRestoreSync)
        {
            if (_scheduledHeatmapRestore is { IsCompleted: false })
                return _scheduledHeatmapRestore;
            _scheduledHeatmapRestore = RunScheduledHeatmapRestoreAsync(cancellationToken);
            return _scheduledHeatmapRestore;
        }
    }

    private async Task RunScheduledHeatmapRestoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _idlingScheduler.RunAsync(_ => RestoreActiveHeatmap(), cancellationToken);
        }
        catch (Exception)
        {
            // The heat map logs a failure and keeps the session for a later retry.
        }
        finally
        {
            lock (_heatmapRestoreSync) _scheduledHeatmapRestore = null;
        }
    }

    // Mutations happen on the Revit thread, the Idling scheduler and WPF handlers, so the refresh
    // marshals to the view like the finding visualization does.
    private void RefreshHeatmapState(bool isActive)
    {
        Dispatcher? dispatcher = _dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted)
        {
            IsHeatmapActive = isActive;
            return;
        }

        if (!dispatcher.CheckAccess())
        {
            _ = dispatcher.InvokeAsync(() => RefreshHeatmapState(isActive));
            return;
        }

        IsHeatmapActive = isActive;
    }
}
