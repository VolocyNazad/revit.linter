// Visualization pipelines, their active state and scheduled restoration.

using Microsoft.Extensions.Logging;
using Revit.Async;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using System.Windows.Threading;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

internal sealed partial class DiagnosticReportViewModel
{
    private IReadOnlyList<VisualizationPipelineViewModel> CreateVisualizationPipelines(DiagnosticReport report)
    {
        if (report.Target is not Element target)
        {
            _logger.LogWarning(
                "Visualization is unavailable for diagnostic {DiagnosticCode}: report target is not a Revit element",
                report.Code);
            return [];
        }

        // Pipelines are built lazily when the UI first shows the row, by which time the target
        // may have been deleted or undone.
        ElementId? maybeTargetId = TryGetElementId(target);
        if (maybeTargetId is null)
        {
            _logger.LogWarning(
                "Visualization is unavailable for diagnostic {DiagnosticCode}: target element is no longer valid",
                report.Code);
            return [];
        }

        ElementDiagnosticRegistration? registration = GetCatalogSnapshot().ElementDiagnostics
            .SingleOrDefault(item => string.Equals(
                item.Identity.Code, report.Code, StringComparison.Ordinal));
        if (registration is null)
        {
            _logger.LogWarning(
                "Visualization is unavailable for diagnostic {DiagnosticCode}: registration was not found",
                report.Code);
            return [];
        }

        IReadOnlyList<IElementVisualizationPipeline> pipelines =
            SelectVisualizationPipelines(registration, target);
        _logger.LogDebug(
            "Created {VisualizationCount} visualization pipelines for diagnostic {DiagnosticCode}",
            pipelines.Count, report.Code);

        VisualizationRequest request = new(
            report,
            target,
            maybeTargetId,
            report.TargetDependencies?.OfType<Element>().ToArray() ?? [],
            pipelines.Count);

        return pipelines
            .Select(pipeline => new VisualizationPipelineViewModel
            {
                Title = pipeline.Value,
                Pipeline = pipeline,
                ShowDelegate = (selectedFromMenu, cancellationToken) =>
                    ShowVisualizationAsync(request, pipeline, selectedFromMenu, cancellationToken)
            })
            .ToArray();
    }

    // A finding whose target is a view is visualized by opening that view: the target-view pipeline
    // replaces the pipelines that would run in the three-dimensional visualization view. A configured
    // rule keeps the pipelines it declares, because the product rules of built-in findings do not
    // apply to it.
    private static IReadOnlyList<IElementVisualizationPipeline> SelectVisualizationPipelines(
        ElementDiagnosticRegistration registration,
        Element target)
    {
        IElementVisualizationPipeline[] targetViewPipelines = registration.VisualizationPipelines
            .Where(pipeline => pipeline is ITargetViewVisualizationPipeline)
            .ToArray();
        if (target is View && targetViewPipelines.Length > 0)
            return targetViewPipelines;

        IElementVisualizationPipeline[] otherPipelines = registration.VisualizationPipelines
            .Where(pipeline => pipeline is not ITargetViewVisualizationPipeline)
            .ToArray();
        return otherPipelines.Length > 0 ? otherPipelines : registration.VisualizationPipelines;
    }

    private async Task ShowVisualizationAsync(
        VisualizationRequest request,
        IElementVisualizationPipeline pipeline,
        bool selectedFromMenu,
        CancellationToken cancellationToken)
    {
        _preferredVisualizationName = pipeline.Value;
        if (!CanShowVisualization(request, pipeline)) return;

        try
        {
            (bool success, bool restoredOnly) = await RevitTask.RunAsync(
                _ => ApplyVisualization(request, pipeline, cancellationToken));

            if (!success && !restoredOnly)
                await _dialog.Show(new DialogRequest(VisualizationFailedMessage), cancellationToken);
            else if (success && !restoredOnly)
                _activityStream.Publish(new VisualizationAppliedActivity(
                    request.Report.Code, selectedFromMenu, request.PipelineCount));
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Visualization {VisualizationName} failed for diagnostic {DiagnosticCode}, target {TargetId}",
                pipeline.Value, request.Report.Code, request.TargetId);
            throw new InvalidOperationException(
                $"Visualization '{pipeline.Value}' failed for diagnostic '{request.Report.Code}'.",
                exception);
        }
    }

    // A report row outlives the Revit state it was built from, so every precondition is checked again
    // right before the pipeline runs, and a request that can no longer be served is logged instead of
    // thrown.
    private bool CanShowVisualization(VisualizationRequest request, IElementVisualizationPipeline pipeline)
    {
        Document document = request.Report.Document;
        if (document is not { IsValidObject: true })
        {
            _logger.LogWarning(
                "Visualization {VisualizationName} skipped for diagnostic {DiagnosticCode}: " +
                "report document is no longer valid",
                pipeline.Value, request.Report.Code);
            return false;
        }

        if (request.Target is not { IsValidObject: true })
        {
            _logger.LogWarning(
                "Visualization {VisualizationName} skipped for diagnostic {DiagnosticCode}: " +
                "target element is no longer valid",
                pipeline.Value, request.Report.Code);
            return false;
        }

        _logger.LogInformation(
            "Visualization requested: {VisualizationName}, diagnostic {DiagnosticCode}, target {TargetId}",
            pipeline.Value, request.Report.Code, request.TargetId);

        Document? activeDocument = _revitContext.ActiveDocument;
        if (activeDocument is null || !activeDocument.Equals(document))
        {
            _logger.LogWarning(
                "Visualization {VisualizationName} skipped for diagnostic {DiagnosticCode}: " +
                "report document is not active",
                pipeline.Value, request.Report.Code);
            return false;
        }

        return true;
    }

    // Runs on the Revit API context: switches between the active visualization and the requested one,
    // then applies the pipeline.
    private (bool Success, bool RestoredOnly) ApplyVisualization(
        VisualizationRequest request,
        IElementVisualizationPipeline pipeline,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Document document = request.Report.Document;
        Document? currentDocument = _revitContext.ActiveDocument;
        if (currentDocument is null || !currentDocument.Equals(document))
            throw new InvalidOperationException("The report document is no longer active.");
        if (!request.Target.IsValidObject)
            throw new InvalidOperationException("The visualization target is no longer valid.");

        if (ReferenceEquals(_activeVisualizationPipeline, pipeline)
            && _activeVisualizationTargetId?.Equals(request.TargetId) == true)
        {
            _logger.LogInformation(
                "Visualization {VisualizationName} for diagnostic {DiagnosticCode} is already active; " +
                "restoring it",
                pipeline.Value, request.Report.Code);
            RestoreActiveVisualization();
            return (true, true);
        }

        RestorePreviousVisualization(pipeline);

        bool applied = ExecuteVisualizationMutation(() => ApplyPipeline(request, pipeline));
        if (applied)
        {
            _activeVisualizationPipeline = pipeline;
            _activeVisualizationTargetId = request.TargetId;
            RefreshVisualizationStates();
        }

        return (applied, false);
    }

    // The previous visualization may no longer be restorable, for example after the user deleted its
    // view or undid it. The pipeline logs that failure, and the request the user has just made must
    // not be blocked by it.
    private void RestorePreviousVisualization(IElementVisualizationPipeline pipeline)
    {
        try
        {
            RestoreActiveVisualization();
            return;
        }
        catch (Exception)
        {
            ClearActiveVisualization();
        }

        _logger.LogWarning(
            "The active visualization could not be restored before showing " +
            "{VisualizationName}; applying the requested visualization anyway",
            pipeline.Value);
    }

    private bool ApplyPipeline(VisualizationRequest request, IElementVisualizationPipeline pipeline)
    {
        Document document = request.Report.Document;
        var elementSets = new Dictionary<string, IReadOnlyCollection<ElementId>>
        {
            [ElementVisualizationSetKeys.Target] = [request.TargetId],
            [ElementVisualizationSetKeys.Dependencies] = request.Dependencies
                .Select(TryGetElementId)
                .OfType<ElementId>()
                .ToArray()
        };

        // Opening the found view is the visualization for a view finding: the target-view pipeline
        // runs in that view, and no three-dimensional visualization view is created.
        if (pipeline is ITargetViewVisualizationPipeline && request.Target is View targetView)
            return pipeline.Apply(new(document, targetView, elementSets, _visualizationViewProvider.Activate));

        // Visualization views cannot be created in family documents,
        // so the pipeline applies to the active view as-is.
        if (document.IsFamilyDocument)
        {
            if (document.ActiveView is not { } activeView)
                throw new InvalidOperationException("The family document has no active view.");
            return pipeline.Apply(new(document, activeView, elementSets));
        }

        View3D visualizationView = _visualizationViewProvider.GetOrCreateView();
        return pipeline.Apply(
            new(document, visualizationView, elementSets, _visualizationViewProvider.Activate));
    }

    private void RestoreActiveVisualization()
    {
        IElementVisualizationPipeline? pipeline = _activeVisualizationPipeline;
        if (pipeline is not null)
        {
            _logger.LogInformation(
                "Restoring active visualization {VisualizationName} for diagnostic {DiagnosticCode}",
                pipeline.Value, pipeline.Identity.Code);
            ExecuteVisualizationMutation(pipeline.Restore);
        }
        ClearActiveVisualization();
    }

    private void ClearActiveVisualization()
    {
        _activeVisualizationPipeline = null;
        _activeVisualizationTargetId = null;
        RefreshVisualizationStates();
    }

    // Marks the rows whose pipeline matches the applied visualization. Mutations happen on the
    // Revit thread, the Idling scheduler and WPF handlers, so the refresh marshals to the view.
    private void RefreshVisualizationStates()
    {
        IElementVisualizationPipeline? pipeline = _activeVisualizationPipeline;
        ElementId? targetId = _activeVisualizationTargetId;
        Dispatcher? dispatcher = _dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted) return;
        if (!dispatcher.CheckAccess())
        {
            _ = dispatcher.InvokeAsync(RefreshVisualizationStates);
            return;
        }

        foreach (DiagnosticReportItemViewModel item in Collection.ToArray())
            item.RefreshVisualizationState(pipeline, targetId);
    }

    private T ExecuteVisualizationMutation<T>(Func<T> action)
    {
        _visualizationMutationDepth++;
        try
        {
            return action();
        }
        finally
        {
            _visualizationMutationDepth--;
        }
    }

    private void ExecuteVisualizationMutation(Action action) =>
        ExecuteVisualizationMutation(() =>
        {
            action();
            return true;
        });

    // Restoring a visualization starts a transaction, which Revit allows only in an API context, while the
    // report can be cleared from a WPF handler or a configuration-change notification. A failed restore
    // must not abort the clearing, so it is retried on the next Idling event; the pipeline keeps the
    // sessions it could not restore and has already logged the failure.
    private void RestoreActiveVisualizationOrSchedule()
    {
        if (_activeVisualizationPipeline is null) return;

        try
        {
            RestoreActiveVisualization();
        }
        catch (Exception)
        {
            _ = RestoreActiveVisualizationAsync();
        }
    }

    private Task RestoreActiveVisualizationAsync(CancellationToken cancellationToken = default)
    {
        if (_activeVisualizationPipeline is null) return Task.CompletedTask;

        // Concurrent Revit events share one scheduled restoration instead of queuing a task per
        // event: the in-flight run restores whatever pipeline is active when it executes, so every
        // trigger is covered without repeating the same failure streak on every event.
        lock (_restoreScheduleSync)
        {
            if (_scheduledRestore is { IsCompleted: false })
                return _scheduledRestore;
            _scheduledRestore = RunScheduledRestoreAsync(cancellationToken);
            return _scheduledRestore;
        }
    }

    private async Task RunScheduledRestoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _idlingScheduler.RunAsync(_ => RestoreActiveVisualization(), cancellationToken);
        }
        catch (Exception)
        {
            // The pipeline logs the failure and retains sessions that still need restoration.
        }
        finally
        {
            lock (_restoreScheduleSync) _scheduledRestore = null;
        }
    }

    // Everything one row needs to apply a pipeline, captured when the row is created so the pipeline
    // delegate does not close over the whole view model state.
    private sealed record VisualizationRequest(
        DiagnosticReport Report,
        Element Target,
        ElementId TargetId,
        Element[] Dependencies,
        int PipelineCount);
}
