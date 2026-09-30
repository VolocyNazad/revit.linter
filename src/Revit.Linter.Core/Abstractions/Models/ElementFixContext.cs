namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>
/// Provides the document and named element sets consumed by an element-fix pipeline.
/// </summary>
/// <param name="Document">The document that owns the elements being fixed.</param>
/// <param name="ElementSets">Named element groups available to pipeline steps.</param>
public sealed record ElementFixContext(
    Document Document,
    IReadOnlyDictionary<string, IReadOnlyCollection<ElementId>> ElementSets);
