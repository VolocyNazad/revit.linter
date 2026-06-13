using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.UserDiagnostics;

internal sealed class ElementDiagnostic(ElementFunctionFactory elementFunctionFactory) : IElementDiagnostic
{
    public required ElementDiagnosticId Identity { get; init; }
    public required string Formula { get; init; }
    public DiagnosticResult Execute(Document document, View? view, Element targetElement) 
        => Delegate.Invoke(targetElement) ? new(DiagnosticVerdict.Valid) : new(DiagnosticVerdict.NotValid);

    private Func<Element, bool> Delegate => field ??= elementFunctionFactory.Create(Formula);
}
