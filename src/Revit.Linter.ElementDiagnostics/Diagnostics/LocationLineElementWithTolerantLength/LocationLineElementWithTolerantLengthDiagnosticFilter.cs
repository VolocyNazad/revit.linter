using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.LocationLineElementWithTolerantLength;

internal sealed class LocationLineElementWithTolerantLengthDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LocationLineElementWithTolerantLength;

    public bool IsRelevantFor(Document document, Element element)
        => element.Location is LocationCurve locationCurve && locationCurve.Curve is Line;
}