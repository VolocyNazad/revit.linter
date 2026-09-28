namespace Revit.Linter.ElementDiagnostics.Diagnostics.ModelCurveExists;

internal sealed class DeleteModelCurve : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ModelCurveExists;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteModelCurve_fix");
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
