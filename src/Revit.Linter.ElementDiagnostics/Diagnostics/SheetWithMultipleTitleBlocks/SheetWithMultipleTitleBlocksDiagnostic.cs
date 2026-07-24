namespace Revit.Linter.ElementDiagnostics.Diagnostics.SheetWithMultipleTitleBlocks;

internal sealed class SheetWithMultipleTitleBlocksDiagnostic : IElementDiagnostic
{
    private readonly ElementFilter _titleBlockFilter =
        new ElementCategoryFilter(BuiltInCategory.OST_TitleBlocks);

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.SheetWithMultipleTitleBlocks;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
        => targetElement.GetDependentElements(_titleBlockFilter).Count > 1
            ? new(DiagnosticVerdict.NotValid)
            : new(DiagnosticVerdict.Valid);
}
