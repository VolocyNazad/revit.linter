namespace Revit.Linter.ElementDiagnostics.Diagnostics.ModelCurveExists;

internal sealed class DeteteModelCurve : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ModelCurveExists;

    public string Value => "Удалить линию модели";
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
