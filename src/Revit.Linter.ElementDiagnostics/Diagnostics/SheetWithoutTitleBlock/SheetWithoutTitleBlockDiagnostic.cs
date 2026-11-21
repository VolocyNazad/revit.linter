namespace Revit.Linter.ElementDiagnostics.Diagnostics.SheetWithoutTitleBlock;

internal sealed class SheetWithoutTitleBlockDiagnostic : IElementDiagnostic
{
    private readonly ElementFilter _titleBlockFilter =
        new ElementCategoryFilter(BuiltInCategory.OST_TitleBlocks);

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.SheetWithoutTitleBlock;

    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
        => DiagnosticFeedbacks.Of(targetElement.GetDependentElements(_titleBlockFilter).Count == 0
            ? new(DiagnosticVerdict.NotValid)
            : new(DiagnosticVerdict.Valid));
}
