using Revit.Sugar;
using Revit.Linter.Core.Abstractions;
using Revit.Linter.ParameterElementDiagnostics.Infrastructure.Utils;
using Revit.Linter.ParameterElementDiagnostics.Models;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ParameterElementDiagnostics;

internal sealed class DocumentDiagnostic(
    IDocumentQueryService documentQueries) : IDocumentDiagnostic
{
    private const string ParameterElementsByNameQuery = "parameter-element-diagnostics:parameter-elements-by-name";
    private const string SharedParameterElementsByGuidQuery = "parameter-element-diagnostics:shared-parameter-elements-by-guid";

    public required DocumentDiagnosticId Identity { get; init; }

    public required IEnumerable<ParameterElementData> Parameters { get; init; }

    public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument)
    {
        if (targetDocument.IsFamilyDocument)
            return [new(DiagnosticVerdict.NotValid, new() {
                { "details", ParameterElementDiagnosticLocalizations.GetString("familyDocumentNotSupported_message") }
            })];
        ICollection<string> messages = [];
        // Group and category identifiers repeat across rules, so they are parsed once per
        // distinct value instead of once per rule.
#if BEFORE2024
        Dictionary<string, BuiltInParameterGroup> parsedGroups = new(StringComparer.Ordinal);
#else
        Dictionary<string, ForgeTypeId> parsedGroups = new(StringComparer.Ordinal);
#endif
        Dictionary<string, BuiltInCategory> parsedCategories = new(StringComparer.Ordinal);
        Dictionary<string, ParameterElement>? parameterElementsByName = null;
        Dictionary<Guid, SharedParameterElement>? sharedParameterElementsByGuid = null;
        BuiltInCategory GetParsedCategory(string name)
        {
            if (!parsedCategories.TryGetValue(name, out BuiltInCategory category))
                parsedCategories[name] = category = ParameterIdentifierParser.ParseCategory(name);
            return category;
        }
        foreach (ParameterElementData parameterData in Parameters)
        {
            ParameterElement? target;
            if (parameterData.Guid is null or "")
            {
                parameterElementsByName ??= GetParameterElementsByName(targetDocument);
                if (!parameterElementsByName.TryGetValue(parameterData.Name, out target)) {
                    messages.Add(ParameterElementDiagnosticLocalizations.GetString(
                        "parameterNotFound_message", parameterData.Name));
                    continue;
                }
            }
            else
            {
                // Shared parameters are a subset of the parameter elements, so they are taken from
                // that cached list instead of a second document-wide collector.
                // The guid is parsed once per rule instead of once per compared element below.
                Guid targetGuid = Guid.Parse(parameterData.Guid);
                sharedParameterElementsByGuid ??= GetSharedParameterElementsByGuid(targetDocument);
                if (!sharedParameterElementsByGuid.TryGetValue(targetGuid, out SharedParameterElement? shared)) {
                    messages.Add(ParameterElementDiagnosticLocalizations.GetString(
                        "sharedParameterNotFound_message", parameterData.Name, parameterData.Guid));
                    continue;
                }
                target = shared;
            }

            BindingMap bindingMap = targetDocument.ParameterBindings;
            InternalDefinition definition = target.GetDefinition();
            // A parameter can exist in the document (for example, through a loaded family)
            // without being bound to categories as a project parameter; the map then returns null.
            if (bindingMap.get_Item(definition) is not ElementBinding binging)
            {
                messages.Add(ParameterElementDiagnosticLocalizations.GetString(
                    "parameterNotBound_message", parameterData.Name, parameterData.Guid ?? string.Empty));
                continue;
            }

            if (definition.Name != parameterData.Name)
                messages.Add(GetInvalidPropertyMessage(parameterData, "Name"));
            if (binging is InstanceBinding && !parameterData.IsInstance)
                messages.Add(GetInvalidPropertyMessage(parameterData, "IsInstance"));
            if (binging is TypeBinding && parameterData.IsInstance)
                messages.Add(GetInvalidPropertyMessage(parameterData, "IsInstance"));
            if (definition.VariesAcrossGroups != parameterData.AllowVaryBetweenGroups)
                messages.Add(GetInvalidPropertyMessage(parameterData, "AllowVaryBetweenGroups"));
#if BEFORE2024
            if (!parsedGroups.TryGetValue(parameterData.Group, out BuiltInParameterGroup group))
                parsedGroups[parameterData.Group] = group = ParameterIdentifierParser.ParseGroup(parameterData.Group);
            if (definition.ParameterGroup != group)
                messages.Add(GetInvalidPropertyMessage(parameterData, "Group"));

            IEnumerable<BuiltInCategory> catgories = parameterData.Categories
                .Select(GetParsedCategory).ToList();
            if (!binging.Categories
                    .Cast<Category>()
                    .Select(i => i.Id.ToBuiltInCategory())
                    .SetEquals(catgories))
                messages.Add(GetInvalidPropertyMessage(parameterData, "Categories"));
#else
            if (!parsedGroups.TryGetValue(parameterData.Group, out ForgeTypeId? group))
                parsedGroups[parameterData.Group] = group = ParameterIdentifierParser.ParseGroupTypeId(parameterData.Group);
            if (definition.GetGroupTypeId() != group)
                messages.Add(GetInvalidPropertyMessage(parameterData, "Group"));

            IEnumerable<BuiltInCategory> catgories = parameterData.Categories
                .Select(GetParsedCategory).ToList();
            if (!binging.Categories
                    .Cast<Category>()
                    .Select(i => (BuiltInCategory)i.Id.Value)
                    .SetEquals(catgories))
                messages.Add(GetInvalidPropertyMessage(parameterData, "Categories"));
#endif
        }

        if (messages.Count == 0)
            return [DiagnosticFeedback.Valid];
        return [new(DiagnosticVerdict.NotValid, new() {
            { "details", string.Join(Environment.NewLine, messages) }
        })];
    }

    private Dictionary<string, ParameterElement> GetParameterElementsByName(Document document)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, ParameterElementsByNameQuery),
            () => BuildParameterElementsByName(document));

    private Dictionary<string, ParameterElement> BuildParameterElementsByName(Document document)
    {
        // Names are resolved once per rule: grouping keeps the linear-search first-wins
        // behavior without scanning the element list per rule.
        IReadOnlyList<ParameterElement> elements = documentQueries.GetElementsOfClass<ParameterElement>(document);
        return elements
            .GroupBy(element => element.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
    }

    private Dictionary<Guid, SharedParameterElement> GetSharedParameterElementsByGuid(Document document)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, SharedParameterElementsByGuidQuery),
            () => BuildSharedParameterElementsByGuid(document));

    private Dictionary<Guid, SharedParameterElement> BuildSharedParameterElementsByGuid(Document document)
    {
        return documentQueries
            .GetElementsOfClass<ParameterElement>(document)
            .OfType<SharedParameterElement>()
            .GroupBy(element => element.GuidValue)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private static string GetInvalidPropertyMessage(ParameterElementData parameterData, string propertyName) =>
        ParameterElementDiagnosticLocalizations.GetString(
            "parameterPropertyInvalid_message",
            parameterData.Name,
            parameterData.Guid ?? string.Empty,
            propertyName);

}
