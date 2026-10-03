using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ParameterElementUnused;

internal sealed class ParameterElementUnusedDiagnostic(
        IDocumentQueryService documentQueries) : IElementDiagnostic
{
    private const string UsedParameterIdsQuery = "element-diagnostics:used-parameter-ids";

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ParameterElementUnused;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        HashSet<ElementId> usedParameterIds = documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, UsedParameterIdsQuery),
            () => BuildUsedParameterIds(document));

        return usedParameterIds.Contains(targetElement.Id)
            ? new(DiagnosticVerdict.Valid)
            : new(DiagnosticVerdict.NotValid);
    }

    // Asking every element for every parameter costs (parameters x elements) Revit API calls. The set of
    // parameters an element has is defined by its category, type and class, so one representative per
    // such combination is read instead and the result is shared by all checked parameters.
    //
    // An element that carries a parameter its type siblings do not have is not seen by this shortcut.
    private HashSet<ElementId> BuildUsedParameterIds(Document document)
    {
        HashSet<ElementId> usedParameterIds = [];
        HashSet<(ElementId? CategoryId, ElementId TypeId, Type Class)> representatives = [];

        foreach (Element element in documentQueries.GetElements(document))
        {
            if (!representatives.Add((element.Category?.Id, element.GetTypeId(), element.GetType())))
                continue;

            foreach (Parameter parameter in element.Parameters)
                usedParameterIds.Add(parameter.Id);
        }

        return usedParameterIds;
    }
}