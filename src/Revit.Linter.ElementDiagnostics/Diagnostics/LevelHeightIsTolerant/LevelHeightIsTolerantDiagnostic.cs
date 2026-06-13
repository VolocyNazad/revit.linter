using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.LevelHeightIsTolerant;

internal sealed class LevelHeightIsTolerantDiagnostic : IElementDiagnostic
{
    private readonly double _tolerance = 0.5;
    private readonly int _roundingDigits = 7;

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LevelHeightIsTolerant;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        var level = (Level)targetElement;

        FormatOptions formatOptions = document.GetUnits().GetFormatOptions(SpecTypeId.Length);
        ForgeTypeId unitTypeId = formatOptions.GetUnitTypeId();

        Parameter parameter = level.get_Parameter(BuiltInParameter.LEVEL_ELEV);
        if (parameter is null) return new(DiagnosticVerdict.Valid);
        double parameterValue = parameter.AsDouble();

        return Math.Round(Math.Abs(UnitUtils.ConvertFromInternalUnits(parameterValue, unitTypeId)), _roundingDigits) % _tolerance != 0
            ? new(DiagnosticVerdict.NotValid) : new(DiagnosticVerdict.Valid);
    }
}
