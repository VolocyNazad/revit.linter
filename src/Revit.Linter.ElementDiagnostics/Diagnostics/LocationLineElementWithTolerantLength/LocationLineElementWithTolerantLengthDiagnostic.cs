using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.LocationLineElementWithTolerantLength;

internal sealed class LocationLineElementWithTolerantLengthDiagnostic : IElementDiagnostic
{
    private readonly double _tolerance = 0.5;
    private readonly int _roundingDigits = 7;

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LocationLineElementWithTolerantLength;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        XYZ basePoint = BasePoint.GetProjectBasePoint(document).Position;

        Line line = (Line)((LocationCurve)targetElement.Location).Curve;

        XYZ firstEnd = line.GetEndPoint(1);
        XYZ secondEnd = line.GetEndPoint(0);

        double[] lengthCollection = [
                firstEnd.X  - secondEnd.X, firstEnd.Y  - secondEnd.Y, firstEnd.Z  - secondEnd.Z,
                secondEnd.X - secondEnd.X, secondEnd.Y - secondEnd.Y, secondEnd.Z - secondEnd.Z,
            ];

        FormatOptions formatOptions = document.GetUnits().GetFormatOptions(SpecTypeId.Length);
        ForgeTypeId unitTypeId = formatOptions.GetUnitTypeId();
        IEnumerable<double> convertedLengthCollection = lengthCollection
            .Select(i => UnitUtils.ConvertFromInternalUnits(i, unitTypeId))
            .Select(Math.Abs)
            .Select(i => Math.Round(i, _roundingDigits)).ToList();

        return convertedLengthCollection.Any(i => i % _tolerance != 0)
            ? new(DiagnosticVerdict.NotValid) : new(DiagnosticVerdict.Valid);
    }
}
