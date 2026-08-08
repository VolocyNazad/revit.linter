namespace Revit.Linter.DocumentDiagnostics.Diagnostics.StartingViewNotSet;

internal sealed class StartingViewNotSetDiagnostic : IDocumentDiagnostic
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.StartingViewNotSet;

    public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument)
    {
        yield return StartingViewSettings.GetStartingViewSettings(targetDocument).ViewId == ElementId.InvalidElementId
            ? new(DiagnosticVerdict.NotValid)
            : DiagnosticFeedback.Valid;
    }
}
