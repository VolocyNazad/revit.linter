using System.Globalization;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.SheetWithMultipleTitleBlocks;

internal sealed class KeepFirstTitleBlock
    : IElementFix, IElementFixConfirmation, IElementFixConfirmationDetails
{
    private readonly ElementFilter _titleBlockFilter =
        new ElementCategoryFilter(BuiltInCategory.OST_TitleBlocks);

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.SheetWithMultipleTitleBlocks;

    public string Value => ElementDiagnosticLocalizations.GetString("keepFirstTitleBlock_fix");

    public string ConfirmationMessage =>
        ElementDiagnosticLocalizations.GetString("keepFirstTitleBlock_confirmationMessage");

    public string ConfirmationButtonText =>
        ElementDiagnosticLocalizations.GetString("keepFirstTitleBlock_confirmationButton");

    public string GetConfirmationMessage(Element targetElement)
    {
        IList<ElementId> titleBlocks = targetElement.GetDependentElements(_titleBlockFilter);
        if (titleBlocks.Count < 2) return ConfirmationMessage;

        // The fix keeps this title block, so the dialog names the one the user will see afterwards.
        string? name = targetElement.Document.GetElement(titleBlocks[0])?.Name;
        if (string.IsNullOrWhiteSpace(name)) return ConfirmationMessage;

        return string.Format(
            CultureInfo.CurrentCulture,
            ElementDiagnosticLocalizations.GetString("keepFirstTitleBlock_confirmationMessageForSheet"),
            name,
            titleBlocks[0]);
    }

    public bool Execute(Element targetElement)
    {
        // The report carries no preferred title block, so the first dependent one is kept.
        IList<ElementId> titleBlocks = targetElement.GetDependentElements(_titleBlockFilter);
        if (titleBlocks.Count < 2) return false;
        ICollection<ElementId> deleted = targetElement.Document.Delete(titleBlocks.Skip(1).ToArray());
        return deleted.Count >= titleBlocks.Count - 1;
    }
}
