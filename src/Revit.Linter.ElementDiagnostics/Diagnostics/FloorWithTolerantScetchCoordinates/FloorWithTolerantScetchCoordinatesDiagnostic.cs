#if AFTER2023
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FloorWithTolerantScetchCoordinates;

internal sealed class FloorWithTolerantScetchCoordinatesDiagnostic : IElementDiagnostic
{
    private readonly double _tolerance = 0.5;
    private readonly int _roundingDigits = 7;

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FloorWithTolerantScetchCoordinates;
    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        var floor = (Floor)targetElement;

        XYZ basePoint = BasePoint.GetProjectBasePoint(document).Position;

        Sketch floorSketch = (Sketch)document.GetElement(floor.SketchId);

        List<double> lengthCollection = [];
        foreach (CurveArray curveArray in floorSketch.Profile)
        {
            foreach (var curve in curveArray)
            {
                if (curve is not Line line) continue;
                XYZ firstEnd = line.GetEndPoint(1);
                XYZ secondEnd = line.GetEndPoint(0);

                lengthCollection.AddRange([
                    firstEnd.X  - basePoint.X, firstEnd.Y  - basePoint.Y, firstEnd.Z  - basePoint.Z,
                    secondEnd.X - basePoint.X, secondEnd.Y - basePoint.Y, secondEnd.Z - basePoint.Z,
                ]);
            }
        }

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
#endif