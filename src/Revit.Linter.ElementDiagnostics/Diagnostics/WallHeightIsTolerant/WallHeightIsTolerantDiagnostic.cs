using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.WallHeightIsTolerant;

internal sealed class WallHeightIsTolerantDiagnostic : IElementDiagnostic
{
    private readonly double _tolerance = 0.5;
    private readonly int _roundingDigits = 7;

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.WallHeightIsTolerant;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        var wall = (Wall)targetElement;

        FormatOptions formatOptions = document.GetUnits().GetFormatOptions(SpecTypeId.Length);
        ForgeTypeId unitTypeId = formatOptions.GetUnitTypeId();

        Parameter parameter = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
        if (parameter is null) return new(DiagnosticVerdict.Valid);
        double parameterValue = parameter.AsDouble();

        return Math.Round(Math.Abs(UnitUtils.ConvertFromInternalUnits(parameterValue, unitTypeId)), _roundingDigits) % _tolerance != 0
            ? new(DiagnosticVerdict.NotValid) : new(DiagnosticVerdict.Valid);
    }
}
