namespace Revit.Linter.ElementDiagnostics.Diagnostics.ArrayWithFormula;

internal sealed class UnbindArrayWithFormula : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ArrayWithFormula;

    public string Value => ElementDiagnosticLocalizations.GetString("unbindArray_fix");
    public bool Execute(Element targetElement)
    {
        if (targetElement is not BaseArray array) return false;
        try
        {
            FamilyParameter? label;
            try
            {
                label = array.Label;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            {
                return false;
            }
            if (label is null) return false;
            // Unbinding keeps the members and drops the parameter (formula) association.
            array.Label = null;
            return true;
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        {
            // Arrays that cannot be unbound (for example, outside a family document)
            // report as not applied, not as a failure. Revit hierarchy, not System.
            return false;
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException)
        {
            return false;
        }
    }
}
