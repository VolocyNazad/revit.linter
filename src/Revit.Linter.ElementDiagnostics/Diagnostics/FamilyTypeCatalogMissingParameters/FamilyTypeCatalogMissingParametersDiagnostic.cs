using Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeCatalog;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeCatalogMissingParameters;

internal sealed class FamilyTypeCatalogMissingParametersDiagnostic : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyTypeCatalogMissingParameters;

    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
    {
        if (!document.IsFamilyDocument || targetElement is not Family || targetElement.Id != document.OwnerFamily.Id)
            return DiagnosticFeedbacks.Valid;
        if (FamilyTypeCatalogReader.TryRead(document) is not { TypeNames.Count: > 0 } catalog)
            return DiagnosticFeedbacks.Valid;

        // A catalog column is the only way a type loaded from the catalog receives a value. A
        // parameter the header does not name keeps its default, so it is missing from the catalog.
        SortedSet<string> missing = new(StringComparer.Ordinal);
        foreach (FamilyParameter parameter in document.FamilyManager.Parameters.Cast<FamilyParameter>())
        {
            if (!FamilyTypeCatalogParameters.IsSetByCatalog(parameter)) continue;
            string name = parameter.Definition.Name;
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!catalog.ContainsParameter(name)) missing.Add(name);
        }

        if (missing.Count == 0) return DiagnosticFeedbacks.Valid;

        return DiagnosticFeedbacks.Of(new(DiagnosticVerdict.NotValid,
            new Dictionary<string, object>
            {
                ["familyName"] = FamilyTypeCatalogReader.ReadFamilyName(document),
                ["catalogName"] = catalog.FileName,
                ["parameterNames"] = string.Join(", ", missing),
            }));
    }
}
