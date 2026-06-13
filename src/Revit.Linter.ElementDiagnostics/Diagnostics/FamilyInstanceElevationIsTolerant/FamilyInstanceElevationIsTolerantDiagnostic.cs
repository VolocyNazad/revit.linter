using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceElevationIsTolerant;

internal sealed class FamilyInstanceElevationIsTolerantDiagnostic : IElementDiagnostic
{
    private readonly double _tolerance = 0.5;
    private readonly int _roundingDigits = 7;

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyInstanceElevationIsTolerant;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        var familyInstance = (FamilyInstance)targetElement;

        FormatOptions formatOptions = document.GetUnits().GetFormatOptions(SpecTypeId.Length);
        ForgeTypeId unitTypeId = formatOptions.GetUnitTypeId();

        Parameter parameter = familyInstance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
        if (parameter is null) return new(DiagnosticVerdict.Valid);
        double parameterValue = parameter.AsDouble();

        return Math.Round(Math.Abs(UnitUtils.ConvertFromInternalUnits(parameterValue, unitTypeId)), _roundingDigits) % _tolerance != 0
            ? new(DiagnosticVerdict.NotValid) : new(DiagnosticVerdict.Valid);
    }
}
