using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ParameterElementDiagnostics.Infrastructure.Extensions;
using Revit.Linter.ParameterElementDiagnostics.Models;
using Revit.TransactionMemoryCache.Abstractions.Services;

namespace Revit.Linter.ParameterElementDiagnostics;

internal sealed class DocumentDiagnostic(
    IRevitTransactionMemoryCache revitTransactionMemoryCache) : IDocumentDiagnostic
{
    public required DocumentDiagnosticId Identity { get; init; }

    public required IEnumerable<ParameterElementData> Parameters { get; init; }

    public DiagnosticResult Execute(Document targetDocument)
    {
        ICollection<string> messages = [];
        foreach (ParameterElementData parameterData in Parameters)
        {
            ParameterElement? target;
            if (parameterData.Guid is null or "")
            {
                List<ParameterElement>? parameterElement = revitTransactionMemoryCache
                  .GetOrCreate($"parameter-elements:document:{targetDocument.Title}\"", () =>
                    new FilteredElementCollector(targetDocument)
                        .OfClass(typeof(ParameterElement)).Cast<ParameterElement>().ToList())
                   ?? throw new InvalidOperationException($"Failed to get object from cache.");

                target = parameterElement.FirstOrDefault(i => i.Name == parameterData.Name);
                if (target is null) {
                    messages.Add($"parameter name: '{parameterData.Name}'. Not exists.");
                    continue;
                }
            }
            else
            {
                List<SharedParameterElement>? parameterElement = revitTransactionMemoryCache
                    .GetOrCreate($"shared-parameter-elements:document:{targetDocument.Title}\"", () =>
                      new FilteredElementCollector(targetDocument)
                          .OfClass(typeof(SharedParameterElement)).Cast<SharedParameterElement>().ToList())
                     ?? throw new InvalidOperationException($"Failed to get object from cache.");

                target = parameterElement.FirstOrDefault(i => i.GuidValue == Guid.Parse(parameterData.Guid));
                if (target is null) {
                    messages.Add($"parameter id: '{parameterData.Guid}' parameter name: '{parameterData.Name}'. Not exists.");
                    continue;
                }
            }

            BindingMap bindingMap = targetDocument.ParameterBindings;
            InternalDefinition definition = target.GetDefinition();
            var binging = (ElementBinding)bindingMap.get_Item(definition);

            if (definition.Name != parameterData.Name)
                messages.Add($"parameter id: '{parameterData.Guid}' parameter name: '{parameterData.Name}'. Not valid 'Name'.");
            if (binging is InstanceBinding && !parameterData.IsInstance) 
                messages.Add($"parameter id: '{parameterData.Guid}' parameter name: '{parameterData.Name}'. Not valid 'IsInstance'.");
            if (binging is TypeBinding && parameterData.IsInstance)
                messages.Add($"parameter id: '{parameterData.Guid}' parameter name: '{parameterData.Name}'. Not valid 'IsInstance'.");
            if (definition.VariesAcrossGroups != parameterData.AllowVaryBetweenGroups)
                messages.Add($"parameter id: '{parameterData.Guid}' parameter name: '{parameterData.Name}'. Not valid 'AllowVaryBetweenGroups'.");
#if BEFORE2024
            var group = int.TryParse(parameterData.Group, out int id) 
                ? (BuiltInParameterGroup)Enum.Parse(typeof(BuiltInParameterGroup), parameterData.Group)
                : (BuiltInParameterGroup)id;
            if (definition.ParameterGroup != group)
                messages.Add("parameter id: '{parameterData.Guid}' parameter name: '{parameterData.Name}'. Not valid 'Group'.");

            IEnumerable<BuiltInCategory> catgories = parameterData.Categories
                .Select(i => {
                    if (int.TryParse(i, out int id)) return (BuiltInCategory)id;
                    return (BuiltInCategory)Enum.Parse(typeof(BuiltInCategory), i);
                }).ToList();
            if (binging.Categories
                    .Cast<Category>()
                    .Select(i => (BuiltInCategory)i.Id.Value())
                    .SetEquals(catgories) == false)
                messages.Add("parameter id: '{parameterData.Guid}' parameter name: '{parameterData.Name}'. Not valid 'Categories'.");
#else
            var group = new ForgeTypeId(parameterData.Group);
            if (definition.GetGroupTypeId() != group)
                messages.Add($"parameter id: '{parameterData.Guid}' parameter name: '{parameterData.Name}'. Not valid 'Group'.");

            IEnumerable<BuiltInCategory> catgories = parameterData.Categories
                .Select(i => {
                    if (long.TryParse(i, out long id)) return (BuiltInCategory)id;
                    return Enum.Parse<BuiltInCategory>(i);
                }).ToList();
            if (binging.Categories
                .Cast<Category>()
                .Select(i => (BuiltInCategory)i.Id.Value)
                .SetEquals(catgories) == false)
                messages.Add($"parameter id: '{parameterData.Guid}' parameter name: '{parameterData.Name}'. Not valid 'Categories'.");
#endif
        }

        if (messages.Count == 0)
            return new(DiagnosticVerdict.Valid);
        return new(DiagnosticVerdict.NotValid, new() { 
            { "details", string.Join(Environment.NewLine, messages) } 
        });
    }
}
