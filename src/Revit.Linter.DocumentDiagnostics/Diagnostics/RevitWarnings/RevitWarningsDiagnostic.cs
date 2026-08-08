namespace Revit.Linter.DocumentDiagnostics.Diagnostics.RevitWarnings;

internal sealed class RevitWarningsDiagnostic : IDocumentDiagnostic
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.RevitWarnings;

    public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument)
    {
        foreach (FailureMessage warning in targetDocument.GetWarnings())
        {
            yield return new DiagnosticFeedback(
                DiagnosticVerdict.NotValid,
                new Dictionary<string, object>
                {
                    ["elementIds"] = warning.GetFailingElements(),
                    ["details"] = warning.GetDescriptionText(),
                });
        }
    }
}
