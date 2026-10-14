namespace Revit.Linter.ElementDiagnostics.Diagnostics.SheetWithMultipleTitleBlocks;

internal sealed class KeepFirstTitleBlock : IElementFix
{
    private readonly ElementFilter _titleBlockFilter =
        new ElementCategoryFilter(BuiltInCategory.OST_TitleBlocks);

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.SheetWithMultipleTitleBlocks;

    public string Value => ElementDiagnosticLocalizations.GetString("keepFirstTitleBlock_fix");
    public bool Execute(Element targetElement)
    {
        // The report carries no preferred title block, so the first dependent one is kept.
        IList<ElementId> titleBlocks = targetElement.GetDependentElements(_titleBlockFilter);
        if (titleBlocks.Count < 2) return false;
        ICollection<ElementId> deleted = targetElement.Document.Delete(titleBlocks.Skip(1).ToArray());
        return deleted.Count >= titleBlocks.Count - 1;
    }
}
