using Revit.Sugar;
using Revit.Linter.ParameterElementDiagnostics.Infrastructure.Utils;
using Revit.Linter.ParameterElementDiagnostics.Models;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ParameterElementDiagnostics;

internal sealed class DocumentDiagnostic(
    IDocumentQueryService documentQueries) : IDocumentDiagnostic
{
    private const string SharedParameterElementsQuery = "parameter-element-diagnostics:shared-parameter-elements";

    public required DocumentDiagnosticId Identity { get; init; }

    public required IEnumerable<ParameterElementData> Parameters { get; init; }

    public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument)
    {
        if (targetDocument.IsFamilyDocument)
            return [new(DiagnosticVerdict.NotValid, new() {
                { "details", ParameterElementDiagnosticLocalizations.GetString("familyDocumentNotSupported_message") }
            })];
        ICollection<string> messages = [];
        foreach (ParameterElementData parameterData in Parameters)
        {
            ParameterElement? target;
            if (parameterData.Guid is null or "")
            {
                IReadOnlyList<ParameterElement> parameterElement =
                    documentQueries.GetElementsOfClass<ParameterElement>(targetDocument);

                target = parameterElement.FirstOrDefault(i => i.Name == parameterData.Name);
                if (target is null) {
                    messages.Add(ParameterElementDiagnosticLocalizations.GetString(
                        "parameterNotFound_message", parameterData.Name));
                    continue;
                }
            }
            else
            {
                // Shared parameters are a subset of the parameter elements, so they are taken from
                // that cached list instead of a second document-wide collector.
                IReadOnlyList<SharedParameterElement> parameterElement = documentQueries.GetOrCreate(
                    DocumentQueryKey.Create(targetDocument, SharedParameterElementsQuery),
                    () => documentQueries.GetElementsOfClass<ParameterElement>(targetDocument)
                        .OfType<SharedParameterElement>()
                        .ToArray());

                target = parameterElement.FirstOrDefault(i => i.GuidValue == Guid.Parse(parameterData.Guid));
                if (target is null) {
                    messages.Add(ParameterElementDiagnosticLocalizations.GetString(
                        "sharedParameterNotFound_message", parameterData.Name, parameterData.Guid));
                    continue;
                }
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
            BuiltInParameterGroup group = ParameterIdentifierParser.ParseGroup(parameterData.Group);
            if (definition.ParameterGroup != group)
                messages.Add(GetInvalidPropertyMessage(parameterData, "Group"));

            IEnumerable<BuiltInCategory> catgories = parameterData.Categories
                .Select(ParameterIdentifierParser.ParseCategory).ToList();
            if (!binging.Categories
                    .Cast<Category>()
                    .Select(i => i.Id.ToBuiltInCategory())
                    .SetEquals(catgories))
                messages.Add(GetInvalidPropertyMessage(parameterData, "Categories"));
#else
            ForgeTypeId group = ParameterIdentifierParser.ParseGroupTypeId(parameterData.Group);
            if (definition.GetGroupTypeId() != group)
                messages.Add(GetInvalidPropertyMessage(parameterData, "Group"));

            IEnumerable<BuiltInCategory> catgories = parameterData.Categories
                .Select(ParameterIdentifierParser.ParseCategory).ToList();
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

    private static string GetInvalidPropertyMessage(ParameterElementData parameterData, string propertyName) =>
        ParameterElementDiagnosticLocalizations.GetString(
            "parameterPropertyInvalid_message",
            parameterData.Name,
            parameterData.Guid ?? string.Empty,
            propertyName);

}
