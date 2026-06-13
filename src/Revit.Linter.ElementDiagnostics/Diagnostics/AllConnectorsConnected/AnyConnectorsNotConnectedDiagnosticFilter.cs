using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.AllConnectorsConnected;

internal sealed class AnyConnectorsNotConnectedDiagnosticFilter : IElementDiagnosticFilter
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.AnyConnectorsNotConnected;

    public bool IsRelevantFor(Document document, Element element) => element is MEPCurve or FamilyInstance;
}
