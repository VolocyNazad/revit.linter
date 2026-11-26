using Revit.Linter.DocumentQueries;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Sugar;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateParameterNames;

internal sealed class DuplicateParameterNamesDiagnostic(IDocumentQueryService documentQueries)
    : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DuplicateParameterNames;

    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
    {
        // A family defines its parameters once for all its types, and a project parameter binding
        // covers a whole category, so every type and every instance of one family repeats the same
        // answer. The check runs on the family element and reports it once per family.
        if (targetElement is not Family family) return DiagnosticFeedbacks.Valid;

        ElementId? categoryId = family.FamilyCategory?.Id;
        if (categoryId is null) return DiagnosticFeedbacks.Valid;

        ProjectParameterCatalog catalog = ProjectParameterCatalog.Get(documentQueries, document);
        IReadOnlyList<ParameterDuplicate> duplicates = FindDuplicates(document, family, categoryId, catalog);
        if (duplicates.Count == 0) return DiagnosticFeedbacks.Valid;

        List<DiagnosticFeedback> feedbacks = new(duplicates.Count);
        foreach (ParameterDuplicate duplicate in duplicates)
        {
            feedbacks.Add(new DiagnosticFeedback(
                DiagnosticVerdict.NotValid,
                new Dictionary<string, object>
                {
                    ["parameter.name"] = duplicate.Name,
                    ["parameter.count"] = duplicate.Count,
                    ["parameter.boundCount"] = duplicate.BoundCount,
                }));
        }

        return feedbacks;
    }

    // A family parameter and a project parameter are different definitions that Revit never merges
    // by name: the family then carries both, and one of them stays empty. Two project definitions
    // with the same name are the document check DOC004, so a name that only has project definitions
    // is skipped here: this check reports the names a family adds to the project.
    private static IReadOnlyList<ParameterDuplicate> FindDuplicates(
        Document document, Family family, ElementId categoryId, ProjectParameterCatalog catalog)
    {
        SortedSet<string> familyNames = CollectFamilyParameterNames(document, family, categoryId, catalog);
        if (familyNames.Count == 0) return [];

        List<ParameterDuplicate> duplicates = [];
        foreach (string name in familyNames)
        {
            int boundCount = catalog.CountBoundTo(name, categoryId);
            if (boundCount == 0) continue;

            // The family contributes one field and the project adds one per bound definition.
            duplicates.Add(new ParameterDuplicate(name, boundCount + 1, boundCount));
        }

        return duplicates;
    }

    // Family parameters are the same on every type, and a project instance parameter is the same on
    // every instance, so one type and one instance describe the whole family. A family without a
    // placed instance still reports its type parameters.
    private static SortedSet<string> CollectFamilyParameterNames(
        Document document, Family family, ElementId categoryId, ProjectParameterCatalog catalog)
    {
        SortedSet<string> names = new(StringComparer.Ordinal);
        foreach (ElementId symbolId in family.GetFamilySymbolIds())
        {
            if (document.GetElement(symbolId) is not Element symbol) continue;
            Collect(symbol);
            break;
        }

        if (FindInstance(document, family) is FamilyInstance instance)
            Collect(instance);

        return names;

        void Collect(Element element)
        {
            foreach (Parameter parameter in element.Parameters)
            {
                if (!TryReadDefinition(parameter, out InternalDefinition definition)) continue;
                if (catalog.IsDefinitionBoundTo(definition.Id, categoryId)) continue;

                names.Add(definition.Name);
            }
        }
    }

    private static FamilyInstance? FindInstance(Document document, Family family)
    {
        ElementFilter filter = ElementFilterUtils.AllFilter();
        foreach (ElementId id in family.GetDependentElements(filter))
        {
            if (document.GetElement(id) is not FamilyInstance instance) continue;
            try
            {
                FamilySymbol? symbol = instance.Symbol;
                if (symbol is null || symbol.Family.Id == family.Id) return instance;
            }
            catch (Exception)
            {
                // An in-place family instance has no readable symbol, and the dependent list already
                // ties it to the family.
                return instance;
            }
        }

        return null;
    }

    // Built-in parameters are excluded by their definition: the name of a built-in parameter is
    // translated, and a custom parameter may legally repeat it.
    private static bool TryReadDefinition(Parameter parameter, out InternalDefinition definition)
    {
        definition = null!;
        try
        {
            if (parameter.Definition is not InternalDefinition candidate) return false;
            if (candidate.BuiltInParameter != BuiltInParameter.INVALID) return false;
            if (string.IsNullOrWhiteSpace(candidate.Name)) return false;

            definition = candidate;
            return true;
        }
        catch (Exception)
        {
            // An unreadable definition leaves the family clean instead of failing the whole run.
            return false;
        }
    }

    private sealed record ParameterDuplicate(string Name, int Count, int BoundCount);
}
