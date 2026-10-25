using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ReferenceLineUnused;

internal sealed class ReferenceLineUnusedDiagnostic(IDocumentQueryService documentQueries)
    : IElementDiagnostic
{
    private const string ReferencingDimensionsQuery = "element-diagnostics:dimensions-by-referenced-element";

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ReferenceLineUnused;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        // Multi-segment dimensions never show up in GetDependentElements, so referencing
        // dimensions are resolved through their references instead (cached once per run).
        bool constrained = GetReferencingDimensions(document, targetElement.Id).Any(IsConstrained);
        return constrained ? new(DiagnosticVerdict.Valid) : new(DiagnosticVerdict.NotValid);
    }

    private IReadOnlyCollection<Dimension> GetReferencingDimensions(Document document, ElementId lineId)
    {
        // The map type is shared with ReferencePlaneUnusedDiagnostic through the same query key:
        // keep both identical, or the second reader will fail to cast the cached value.
        Dictionary<ElementId, HashSet<Dimension>> dimensionsByElement = documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, ReferencingDimensionsQuery),
            () => BuildReferenceMap(document));
        return dimensionsByElement.TryGetValue(lineId, out HashSet<Dimension>? dimensions)
            ? dimensions
            : [];
    }

    private Dictionary<ElementId, HashSet<Dimension>> BuildReferenceMap(Document document)
    {
        Dictionary<ElementId, HashSet<Dimension>> dimensionsByElement = [];
        // Dimensions are shared with the other diagnostics through the class query instead of
        // a dedicated collector per map build.
        IReadOnlyList<Dimension> dimensions = documentQueries.GetElementsOfClass<Dimension>(document);
        foreach (Dimension dimension in dimensions)
        {
            foreach (Reference reference in dimension.References)
            {
                if (reference.ElementId == ElementId.InvalidElementId) continue;
                if (!dimensionsByElement.TryGetValue(reference.ElementId, out HashSet<Dimension>? set))
                    dimensionsByElement[reference.ElementId] = set = [];
                set.Add(dimension);
            }
        }
        return dimensionsByElement;
    }

    private static bool IsConstrained(Dimension dimension)
    {
        // Dimension shape getters throw InvalidOperationException when the property does not apply
        // (IsLocked on multi-segment linear dimensions, AreSegmentsEqual elsewhere): every probe
        // below is defensive, and an unreadable state counts as unconstrained.
        if (TryReadLocked(dimension)) return true;
        foreach (DimensionSegment segment in dimension.Segments)
            if (TryReadSegmentLocked(segment)) return true;
        return TryReadEqualized(dimension);
    }

    private static bool TryReadLocked(Dimension dimension)
    {
        try
        {
            return dimension.IsLocked;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        {
            return false;
        }
    }

    private static bool TryReadSegmentLocked(DimensionSegment segment)
    {
        try
        {
            return segment.IsLocked;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        {
            return false;
        }
    }

    private static bool TryReadEqualized(Dimension dimension)
    {
        if (dimension.NumberOfSegments <= 1) return false;
        try
        {
            return dimension.AreSegmentsEqual;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        {
            return false;
        }
    }
}
