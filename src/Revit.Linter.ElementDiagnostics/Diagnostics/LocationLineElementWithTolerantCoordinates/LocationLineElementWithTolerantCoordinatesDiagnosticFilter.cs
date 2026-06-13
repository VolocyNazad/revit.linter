using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.LocationLineElementWithTolerantCoordinates;

internal sealed class LocationLineElementWithTolerantCoordinatesDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LocationLineElementWithTolerantCoordinates;

    public bool IsRelevantFor(Document document, Element element)
        => element.Location is LocationCurve locationCurve && locationCurve.Curve is Line;
}
