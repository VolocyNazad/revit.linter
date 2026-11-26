namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeCatalog;

/// <summary>Decides which family parameters a type catalog can set.</summary>
internal static class FamilyTypeCatalogParameters
{
    /// <summary>
    /// Gets whether the catalog sets the parameter: a catalog sets type parameters only, a formula,
    /// report or read-only parameter cannot be set from it, and a built-in parameter exists in every
    /// family, so it is not a parameter a catalog is expected to describe.
    /// </summary>
    public static bool IsSetByCatalog(FamilyParameter parameter)
    {
        if (parameter.IsInstance
            || !string.IsNullOrWhiteSpace(parameter.Formula)
            || parameter.IsReporting
            || parameter.IsReadOnly)
        {
            return false;
        }

        return parameter.Definition is not InternalDefinition definition
            || definition.BuiltInParameter == BuiltInParameter.INVALID;
    }
}
