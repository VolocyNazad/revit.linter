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
        ElementId visualizationTargetId = maybeTargetId;

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

        _logger.LogDebug(
            "Created {VisualizationCount} visualization pipelines for diagnostic {DiagnosticCode}",
            registration.VisualizationPipelines.Count, report.Code);

        Element[] dependencies = report.TargetDependencies?.OfType<Element>().ToArray() ?? [];
        return registration.VisualizationPipelines.Select(pipeline => new VisualizationPipelineViewModel
        {
            Title = pipeline.Value,
            Pipeline = pipeline,
            ShowDelegate = async (selectedFromMenu, cancellationToken) =>
            {
                _preferredVisualizationName = pipeline.Value;
                Document document = report.Document;
                if (document is not { IsValidObject: true })
                {
                    _logger.LogWarning(
                        "Visualization {VisualizationName} skipped for diagnostic {DiagnosticCode}: " +
                        "report document is no longer valid",
                        pipeline.Value, report.Code);
                    return;
                }
                if (target is not { IsValidObject: true })
                {
                    _logger.LogWarning(
                        "Visualization {VisualizationName} skipped for diagnostic {DiagnosticCode}: " +
                        "target element is no longer valid",
                        pipeline.Value, report.Code);
                    return;
                }
                _logger.LogInformation(
                    "Visualization requested: {VisualizationName}, diagnostic {DiagnosticCode}, target {TargetId}",
                    pipeline.Value, report.Code, visualizationTargetId);
                Document? activeDocument = _revitContext.ActiveDocument;
                if (activeDocument is null || !activeDocument.Equals(document))
                {
                    _logger.LogWarning(
                        "Visualization {VisualizationName} skipped for diagnostic {DiagnosticCode}: " +
                        "report document is not active",
                        pipeline.Value, report.Code);
                    return;
                }

                try
                {
                    (bool success, bool restoredOnly) = await RevitTask.RunAsync(_ =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Document? currentDocument = _revitContext.ActiveDocument;
                        if (currentDocument is null || !currentDocument.Equals(document))
                            throw new InvalidOperationException("The report document is no longer active.");
                        if (!target.IsValidObject)
                            throw new InvalidOperationException("The visualization target is no longer valid.");

                        if (ReferenceEquals(_activeVisualizationPipeline, pipeline)
                            && _activeVisualizationTargetId?.Equals(visualizationTargetId) == true)
                        {
                            _logger.LogInformation(
                                "Visualization {VisualizationName} for diagnostic {DiagnosticCode} is already active; " +
                                "restoring it",
                                pipeline.Value, report.Code);
                            RestoreActiveVisualization();
                            return (true, true);
                        }

                        RestoreActiveVisualization();

                        ElementId[] dependencyIds = dependencies
                            .Select(TryGetElementId)
                            .OfType<ElementId>()
                            .ToArray();
                        var elementSets = new Dictionary<string, IReadOnlyCollection<ElementId>>
                        {
                            [ElementVisualizationSetKeys.Target] = [visualizationTargetId],
                            [ElementVisualizationSetKeys.Dependencies] = dependencyIds
                        };
                        bool applied = ExecuteVisualizationMutation(() =>
                        {
                            // Visualization views cannot be created in family documents,
                            // so the pipeline applies to the active view as-is.
                            if (document.IsFamilyDocument)
                            {
                                if (document.ActiveView is not { } activeView)
                                    throw new InvalidOperationException(
                                        "The family document has no active view.");
                                return pipeline.Apply(new(document, activeView, elementSets));
                            }

                            _visualizationViewActivator.Activate();
                            if (document.ActiveView is not View3D visualizationView)
                                throw new InvalidOperationException(
                                    "The diagnostic visualization view was not activated.");
                            return pipeline.Apply(new(document, visualizationView, elementSets));
                        });
                        if (applied)
                        {
                            _activeVisualizationPipeline = pipeline;
                            _activeVisualizationTargetId = visualizationTargetId;
                            RefreshVisualizationStates();
                        }
                        return (applied, false);
                    });

                    if (!success && !restoredOnly)
                        await _dialog.Show(new DialogRequest(VisualizationFailedMessage), cancellationToken);
                    else if (success && !restoredOnly)
                        _activityStream.Publish(new VisualizationAppliedActivity(
                            report.Code,
                            selectedFromMenu,
                            registration.VisualizationPipelines.Count));
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Visualization {VisualizationName} failed for diagnostic {DiagnosticCode}, target {TargetId}",
                        pipeline.Value, report.Code, visualizationTargetId);
                    throw new InvalidOperationException(
                        $"Visualization '{pipeline.Value}' failed for diagnostic '{report.Code}'.",
                        exception);
                }
            }
        }).ToArray();
    }

    private void RestoreActiveVisualization()
    {
        IElementVisualizationPipeline? pipeline = _activeVisualizationPipeline;
        if (pipeline is not null)
            _logger.LogInformation(
                "Restoring active visualization {VisualizationName} for diagnostic {DiagnosticCode}",
                pipeline.Value, pipeline.Identity.Code);
        if (pipeline is not null)
            ExecuteVisualizationMutation(pipeline.Restore);
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
}

