namespace Revit.Linter.DocumentDiagnostics.Diagnostics.StartingViewNotSet;

internal sealed class StartingViewNotSetDiagnostic : IDocumentDiagnostic
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.StartingViewNotSet;

    public DiagnosticFeedback Execute(Document targetDocument)
        => StartingViewSettings.GetStartingViewSettings(targetDocument).ViewId == ElementId.InvalidElementId
        ? new(DiagnosticVerdict.NotValid) : new(DiagnosticVerdict.Valid);
}
