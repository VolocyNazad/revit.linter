using Revit.Linter.DocumentQueries;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateParameterNames;

internal sealed class DuplicateParameterNamesDiagnostic(IDocumentQueryService documentQueries)
    : IElementDiagnostic
{
    private const string DuplicatesByTypeQuery = "element-diagnostics:duplicate-parameter-names";

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DuplicateParameterNames;

    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
    {
        // The parameter definitions of a family instance come from its type, so every instance of
        // one type repeats the answer: the duplicate names are read once per type and reused.
        IReadOnlyList<ParameterDuplicate> duplicates = FindDuplicates(document, targetElement);
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

    private IReadOnlyList<ParameterDuplicate> FindDuplicates(Document document, Element targetElement)
    {
        ElementId typeId = targetElement.GetTypeId();
        if (typeId == ElementId.InvalidElementId) return ReadDuplicates(document, targetElement);

        return documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, DuplicatesByTypeQuery, elementId: typeId),
            () => ReadDuplicates(document, targetElement));
    }

    // A family parameter and a project parameter are different definitions that Revit never merges
    // by name: the element then carries both, and one of them stays empty. Two project definitions
    // with the same name are the document check DOC004, so a name whose parameters are all bound in
    // the project is skipped here.
    private IReadOnlyList<ParameterDuplicate> ReadDuplicates(Document document, Element targetElement)
    {
        ProjectParameterCatalog catalog = ProjectParameterCatalog.Get(documentQueries, document);
        Dictionary<string, int> counts = CountParametersByName(targetElement);
        if (counts.Count == 0) return [];

        ElementId? categoryId = targetElement.Category?.Id;
        List<ParameterDuplicate> duplicates = [];
        foreach (KeyValuePair<string, int> pair in counts)
        {
            if (pair.Value < 2) continue;

            int boundCount = categoryId is null ? 0 : catalog.CountBoundTo(pair.Key, categoryId);
            if (pair.Value <= boundCount) continue;

            duplicates.Add(new ParameterDuplicate(pair.Key, pair.Value, boundCount));
        }

        return duplicates;
    }

    // Built-in parameters are excluded by their definition: the name of a built-in parameter is
    // translated, and a custom parameter may legally repeat it.
    private static Dictionary<string, int> CountParametersByName(Element targetElement)
    {
        Dictionary<string, int> counts = new(StringComparer.Ordinal);
        foreach (Parameter parameter in targetElement.Parameters)
        {
            if (!TryReadName(parameter, out string name)) continue;
            counts[name] = counts.TryGetValue(name, out int count) ? count + 1 : 1;
        }

        return counts;
    }

    private static bool TryReadName(Parameter parameter, out string name)
    {
        name = string.Empty;
        try
        {
            if (parameter.Definition is not InternalDefinition definition) return false;
            if (definition.BuiltInParameter != BuiltInParameter.INVALID) return false;

            name = definition.Name;
            return !string.IsNullOrWhiteSpace(name);
        }
        catch (Exception)
        {
            // An unreadable definition leaves the element clean instead of failing the whole run.
            return false;
        }
    }

    private sealed record ParameterDuplicate(string Name, int Count, int BoundCount);
}
