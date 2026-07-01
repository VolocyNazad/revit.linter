namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceElevationIsTolerant;

internal sealed class FamilyInstanceElevationIsTolerantDiagnostic : IElementDiagnostic
{
    private const double Tolerance = 0.5;
    private const int RoundingDigits = 7;
    private const double Epsilon = 1e-9;

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyInstanceElevationIsTolerant;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        var familyInstance = (FamilyInstance)targetElement;

        FormatOptions formatOptions = document.GetUnits().GetFormatOptions(SpecTypeId.Length);
        ForgeTypeId unitTypeId = formatOptions.GetUnitTypeId();

        Parameter parameter = familyInstance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
        if (parameter is null) return new(DiagnosticVerdict.Valid);
        double parameterValue = parameter.AsDouble();

        return Math.Round(Math.Abs(UnitUtils.ConvertFromInternalUnits(parameterValue, unitTypeId)), RoundingDigits) % Tolerance < Epsilon
            ? new(DiagnosticVerdict.Valid) : new(DiagnosticVerdict.NotValid);
    }
}
