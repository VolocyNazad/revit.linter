namespace Revit.Linter.ElementDiagnostics.Diagnostics.WallHeightIsTolerant;

internal sealed class WallHeightIsTolerantDiagnostic : IElementDiagnostic
{
    private readonly double Tolerance = 0.5;
    private readonly int RoundingDigits = 7;
    private const double Epsilon = 1e-9;

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.WallHeightIsTolerant;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        var wall = (Wall)targetElement;

        FormatOptions formatOptions = document.GetUnits().GetFormatOptions(SpecTypeId.Length);
        ForgeTypeId unitTypeId = formatOptions.GetUnitTypeId();

        Parameter parameter = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
        if (parameter is null) return new(DiagnosticVerdict.Valid);
        double parameterValue = parameter.AsDouble();

        return Math.Round(Math.Abs(UnitUtils.ConvertFromInternalUnits(parameterValue, unitTypeId)), RoundingDigits) % Tolerance < Epsilon
            ? new(DiagnosticVerdict.Valid) : new(DiagnosticVerdict.NotValid);
    }
}
