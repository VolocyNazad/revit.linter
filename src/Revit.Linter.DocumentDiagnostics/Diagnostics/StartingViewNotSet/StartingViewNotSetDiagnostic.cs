using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.DocumentDiagnostics.Diagnostics.StartingViewNotSet;

internal sealed class StartingViewNotSetDiagnostic : IDocumentDiagnostic
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.StartingViewNotSet;

    public DiagnosticResult Execute(Document targetDocument)
        => StartingViewSettings.GetStartingViewSettings(targetDocument).ViewId == ElementId.InvalidElementId
        ? new(DiagnosticVerdict.NotValid) : new(DiagnosticVerdict.Valid);
}
