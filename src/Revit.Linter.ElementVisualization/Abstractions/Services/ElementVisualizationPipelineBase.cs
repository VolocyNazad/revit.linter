using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementVisualization.Abstractions.Models;
using Revit.Linter.ElementVisualization.Services;

namespace Revit.Linter.ElementVisualization.Abstractions.Services;

/// <summary>Provides ordered execution and reverse-order restoration for visualization steps.</summary>
/// <param name="identity">The diagnostic identity associated with the pipeline.</param>
/// <param name="value">The user-facing pipeline name.</param>
/// <param name="services">The services that execute element accent operations.</param>
/// <param name="overrideElementGraphicsService">Applies per-element graphics.</param>
/// <param name="overrideFilterGraphicsService">Applies graphics through temporary filters.</param>
/// <param name="setCategoryVisibilityService">Leaves only the given categories visible.</param>
/// <param name="viewSettingsService">Temporarily changes whole-view settings.</param>
/// <param name="logger">Writes visualization lifecycle diagnostics.</param>
public abstract class ElementVisualizationPipelineBase(
    ElementDiagnosticId identity,
    string value,
    IEnumerable<IAccentElementsService> services,
    IOverrideElementGraphicsService overrideElementGraphicsService,
    IOverrideFilterGraphicsService overrideFilterGraphicsService,
    ISetCategoryVisibilityService setCategoryVisibilityService,
    IViewSettingsService viewSettingsService,
    ILogger logger) : IElementVisualizationPipeline
{
    private readonly VisualizationStepRunner _stepRunner = new(
        services,
        overrideElementGraphicsService,
        overrideFilterGraphicsService,
        setCategoryVisibilityService,
        viewSettingsService,
        logger);
    private readonly VisualizationRestoreSessions _restoreSessions = new(value, identity.Code, logger);
    private bool _disposed;

    /// <inheritdoc />
    public ElementDiagnosticId Identity { get; } = identity;

    /// <inheritdoc />
    public string Value { get; } = value;

    /// <summary>Gets the steps to execute in declaration order.</summary>
    protected abstract IReadOnlyList<ElementVisualizationStep> Steps { get; }

    /// <inheritdoc />
    public bool Apply(ElementVisualizationContext context)
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        logger.LogInformation(
            "Applying visualization pipeline {VisualizationName} for diagnostic {DiagnosticCode} " +
            "on document {DocumentTitle}, view {ViewName}. Steps: {StepCount}; element sets: {ElementSetCount}",
            Value, Identity.Code, context.Document.Title, context.View.Name, Steps.Count, context.ElementSets.Count);
        Restore();

        List<IRestoreSession> appliedSessions = [];
        Stopwatch stopwatch = Stopwatch.StartNew();
        using VisualizationApplyScope scope = new(
            context.Document, context.View, context.ViewActivator, Value, logger);
        try
        {
            for (int index = 0; index < Steps.Count; index++)
            {
                ElementVisualizationStep step = Steps[index];
                scope.Prepare(step);
                appliedSessions.Add(_stepRunner.Execute(context, step, index + 1, Steps.Count));
            }

            scope.Complete();
            _restoreSessions.Add(context.Document, appliedSessions);
            logger.LogInformation(
                "Visualization pipeline {VisualizationName} for diagnostic {DiagnosticCode} " +
                "applied successfully in {ElapsedMilliseconds:F0} ms",
                Value, Identity.Code, stopwatch.Elapsed.TotalMilliseconds);
            return true;
        }
        catch (Exception exception)
        {
            scope.Rollback();
            Exception? cleanupError = DisposeUnappliedSessions(appliedSessions);
            logger.LogError(
                exception,
                "Visualization pipeline {VisualizationName} for diagnostic {DiagnosticCode} failed; " +
                "restoring {SessionCount} completed sessions",
                Value, Identity.Code, appliedSessions.Count);
            throw new InvalidOperationException(
                $"Visualization pipeline '{Value}' failed for diagnostic '{Identity.Code}'.",
                cleanupError is null ? exception : new AggregateException(exception, cleanupError));
        }
    }

    /// <inheritdoc />
    public void Restore() => _restoreSessions.Restore();

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the visualization state owned by the pipeline.</summary>
    /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
        if (!disposing) return;

        try
        {
            Restore();
        }
        catch
        {
            // Disposal is best-effort. Explicit Restore reports failures to the caller.
        }
    }

    // Sessions of a run that failed after they were created were never stored, so their restore
    // actions must not run; only the resources they hold are released.
    private static Exception? DisposeUnappliedSessions(IEnumerable<IRestoreSession> sessions)
    {
        List<Exception> errors = [];
        foreach (IRestoreSession session in sessions.Reverse())
        {
            try
            {
                session.Dispose();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }
        return errors.Count == 0 ? null : new AggregateException(errors);
    }
}
