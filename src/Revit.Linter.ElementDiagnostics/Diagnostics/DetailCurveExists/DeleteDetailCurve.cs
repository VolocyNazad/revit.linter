namespace Revit.Linter.ElementDiagnostics.Diagnostics.DetailCurveExists;

internal sealed class DeleteDetailCurve : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DetailCurveExists;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteDetailCurve_fix");
    public bool Execute(Element targetElement)
        => targetElement.Document.Delete(targetElement.Id).Any();
}
