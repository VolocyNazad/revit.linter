using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementVisualization;

/// <summary>
/// Opens the view that is the target of a built-in diagnostic finding.
/// </summary>
/// <remarks>
/// The pipeline applies no visualization steps and changes no document state: for a finding whose
/// target is a view, opening that view is the visualization. The report presenter applies the
/// pipeline in the target view and does not create the three-dimensional visualization view. Nothing
/// is captured for restoration, because the view that was active before the application is not part
/// of the document state the other pipelines restore either.
/// </remarks>
public sealed class OpenViewVisualizationPipeline(
    ElementDiagnosticId identity,
    string value,
    ILogger<OpenViewVisualizationPipeline> logger) : ITargetViewVisualizationPipeline
{
    /// <inheritdoc />
    public ElementDiagnosticId Identity { get; } = identity;

    /// <inheritdoc />
    public string Value { get; } = value;

    /// <inheritdoc />
    public bool Apply(ElementVisualizationContext context)
    {
        if (context.View is not { IsValidObject: true } view)
        {
            logger.LogWarning(
                "The target view of visualization pipeline {VisualizationName} for diagnostic " +
                "{DiagnosticCode} is no longer valid",
                Value, Identity.Code);
            return false;
        }

        if (context.ViewActivator is null)
        {
            logger.LogWarning(
                "The target view {ViewName} of visualization pipeline {VisualizationName} for " +
                "diagnostic {DiagnosticCode} was not opened because no view activator was supplied",
                view.Name, Value, Identity.Code);
            return false;
        }

        // Revit rejects a change of the active view while the document is modifiable. The run is
        // treated as applied instead of failing the whole visualization: the next click in a
        // settled document opens the view.
        if (context.Document.IsModifiable)
        {
            logger.LogWarning(
                "The target view {ViewName} of visualization pipeline {VisualizationName} for " +
                "diagnostic {DiagnosticCode} was not opened because a transaction keeps the " +
                "document modifiable",
                view.Name, Value, Identity.Code);
            return true;
        }

        logger.LogInformation(
            "Opening target view {ViewName} for visualization pipeline {VisualizationName} of " +
            "diagnostic {DiagnosticCode}",
            view.Name, Value, Identity.Code);
        context.ViewActivator(view);
        return true;
    }

    /// <inheritdoc />
    public void Restore()
    {
        // Opening a view changes no document state, so there is nothing to restore.
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // The pipeline owns no resources beyond the collaborators the container manages.
    }
}
