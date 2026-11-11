using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ViewHasHiddenElements;

internal sealed class ShowAllHiddenViewElements(IDocumentQueryService documentQueries) : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ViewHasHiddenElements;

    public string Value => ElementDiagnosticLocalizations.GetString("showAllHiddenViewElements_fix");

    public bool Execute(Element targetElement)
    {
        if (targetElement is not View view) return false;

        // The hidden set is read again inside the fix transaction so the fix acts on the current
        // view state instead of the state the finding was produced from.
        List<Element> hidden = HiddenInViewElements.Collect(documentQueries.GetElements(view.Document), view);
        if (hidden.Count == 0) return false;

        try
        {
            view.UnhideElements(hidden.ConvertAll(element => element.Id));
            return true;
        }
        catch (Exception)
        {
            // A view that refuses a change, for example a view whose visibility is driven by its
            // template, reports the fix as not applied instead of failing the fix run.
            return false;
        }
    }
}
