namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>
/// Marks a visualization pipeline that applies in the finding target itself because the target is a view.
/// </summary>
/// <remarks>
/// The report presenter offers such a pipeline only for findings whose target is a <c>View</c>, and it
/// applies the pipeline in that view instead of creating the three-dimensional visualization view.
/// Applying the pipeline therefore opens the found view, which is the whole visualization for a
/// view finding; the built-in registration adds this pipeline to every built-in diagnostic so a check
/// that later starts reporting views needs no visualization of its own.
/// </remarks>
public interface ITargetViewVisualizationPipeline : IElementVisualizationPipeline
{
}
