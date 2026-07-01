namespace Revit.Linter.ElementDiagnostics.Diagnostics.LocationLineElementWithTolerantLength;

internal sealed class LocationLineElementWithTolerantLengthDiagnostic : IElementDiagnostic
{
    private readonly double Tolerance = 0.5;
    private readonly int RoundingDigits = 7;
    private const double Epsilon = 1e-9;

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LocationLineElementWithTolerantLength;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        Line line = (Line)((LocationCurve)targetElement.Location).Curve;

        XYZ firstEnd = line.GetEndPoint(1);
        XYZ secondEnd = line.GetEndPoint(0);

        double[] lengthCollection = [
                firstEnd.X  - secondEnd.X, firstEnd.Y  - secondEnd.Y, firstEnd.Z  - secondEnd.Z,
            ];

        FormatOptions formatOptions = document.GetUnits().GetFormatOptions(SpecTypeId.Length);
        ForgeTypeId unitTypeId = formatOptions.GetUnitTypeId();
        IEnumerable<double> convertedLengthCollection = lengthCollection
            .Select(i => UnitUtils.ConvertFromInternalUnits(i, unitTypeId))
            .Select(Math.Abs)
            .Select(i => Math.Round(i, RoundingDigits)).ToList();

        return convertedLengthCollection.Any(i => i % Tolerance > Epsilon)
            ? new(DiagnosticVerdict.NotValid) : new(DiagnosticVerdict.Valid);
    }
}
