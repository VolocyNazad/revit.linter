namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>
/// Provides the Revit objects and named element sets consumed by a visualization pipeline.
/// </summary>
/// <param name="Document">The document that owns the elements being visualized.</param>
/// <param name="View">The view in which the visualization is applied.</param>
/// <param name="ElementSets">
/// Named element groups available to pipeline steps. A step may combine one or more groups.
/// </param>
/// <param name="ViewActivator">
/// Activates the view so the user sees the visualization. The presenter supplies it because switching
/// the active view is a user interface operation; the pipeline calls it only when no transaction is
/// open, because Revit rejects the switch while the document is modifiable.
/// </param>
public sealed record ElementVisualizationContext(
    Document Document,
    View View,
    IReadOnlyDictionary<string, IReadOnlyCollection<ElementId>> ElementSets,
    Action<View>? ViewActivator = null);

/// <summary>
/// Defines the conventional element-set keys supplied by diagnostic presenters.
/// </summary>
public static class ElementVisualizationSetKeys
{
    /// <summary>
    /// Identifies the elements that directly produced a diagnostic result.
    /// </summary>
    public const string Target = nameof(Target);

    /// <summary>
    /// Identifies related elements required to explain a diagnostic result.
    /// </summary>
    public const string Dependencies = nameof(Dependencies);
}
