namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Describes one element of the findings heat map and the value that decides its colour.</summary>
/// <param name="ElementId">The element to colour in the three-dimensional view.</param>
/// <param name="Severity">The most severe severity among the element's findings.</param>
/// <param name="Count">The number of distinct checks of that severity that the element fails.</param>
public sealed record FindingHeatmapEntry(ElementId ElementId, DiagnosticSeverity Severity, int Count);
