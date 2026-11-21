namespace Revit.Linter.ElementDiagnostics.Diagnostics.LevelHeightIsTolerant;

internal sealed class LevelHeightIsTolerantDiagnostic : IElementDiagnostic
{
    private const double Tolerance = 0.5;
    private const int RoundingDigits = 7;
    private const double Epsilon = 1e-9;

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LevelHeightIsTolerant;

    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
    {
        var level = (Level)targetElement;

        FormatOptions formatOptions = document.GetUnits().GetFormatOptions(SpecTypeId.Length);
        ForgeTypeId unitTypeId = formatOptions.GetUnitTypeId();

        Parameter parameter = level.get_Parameter(BuiltInParameter.LEVEL_ELEV);
        if (parameter is null) return DiagnosticFeedbacks.Valid;
        double parameterValue = parameter.AsDouble();

        return DiagnosticFeedbacks.Of(Math.Round(Math.Abs(UnitUtils.ConvertFromInternalUnits(parameterValue, unitTypeId)), RoundingDigits) % Tolerance < Epsilon
            ? new(DiagnosticVerdict.Valid) : new(DiagnosticVerdict.NotValid));
    }
}
