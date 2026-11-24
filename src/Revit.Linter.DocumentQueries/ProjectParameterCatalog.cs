using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.DocumentQueries;

/// <summary>
/// One project parameter definition and the categories it is bound to.
/// </summary>
/// <param name="Name">The definition name, as the user sees it.</param>
/// <param name="SharedGuid">The shared parameter GUID, or <see langword="null"/> for a project parameter that is not shared.</param>
/// <param name="IsInstance">Whether the definition is bound to instances rather than to types.</param>
/// <param name="CategoryIds">The categories the definition is bound to.</param>
public sealed record ProjectParameterDefinition(
    string Name,
    Guid? SharedGuid,
    bool IsInstance,
    IReadOnlyList<ElementId> CategoryIds);

/// <summary>
/// Reads the project parameter definitions of a document and answers how many of them share a name.
/// </summary>
/// <remarks>
/// A project document keeps every project parameter in <see cref="Document.ParameterBindings"/>, both
/// shared and not shared, so the catalog sees the definitions behind a duplicate name. Family
/// parameters are not there: they belong to the family, not to the project, and reach an element only
/// through a loaded family.
/// </remarks>
public sealed class ProjectParameterCatalog
{
    private const string QueryName = "document:project-parameter-catalog";

    private readonly Dictionary<string, Dictionary<ElementId, int>> _categoryCountsByName;

    private ProjectParameterCatalog(IReadOnlyList<ProjectParameterDefinition> definitions)
    {
        Definitions = definitions;
        _categoryCountsByName = BuildCategoryCounts(definitions);
    }

    /// <summary>
    /// Gets every project parameter definition of the document, in binding map order.
    /// </summary>
    public IReadOnlyList<ProjectParameterDefinition> Definitions { get; }

    /// <summary>
    /// Counts the project parameter definitions with <paramref name="name"/> that are bound to <paramref name="categoryId"/>.
    /// </summary>
    /// <param name="name">The definition name.</param>
    /// <param name="categoryId">The category the element belongs to.</param>
    /// <returns>The number of bound definitions, or 0 when none is bound to the category.</returns>
    public int CountBoundTo(string name, ElementId categoryId) =>
        _categoryCountsByName.TryGetValue(name, out Dictionary<ElementId, int>? categories)
        && categories.TryGetValue(categoryId, out int count)
            ? count
            : 0;

    /// <summary>
    /// Gets the catalog of <paramref name="document"/>, cached until the document changes.
    /// </summary>
    /// <param name="documentQueries">The query service that owns the cache.</param>
    /// <param name="document">The project document to read.</param>
    /// <returns>The project parameter catalog of the document.</returns>
    /// <remarks>The document must not be a family document: <see cref="Document.ParameterBindings"/> throws there.</remarks>
    public static ProjectParameterCatalog Get(IDocumentQueryService documentQueries, Document document) =>
        documentQueries.GetOrCreate(DocumentQueryKey.Create(document, QueryName), () => Create(document));

    private static ProjectParameterCatalog Create(Document document)
    {
        List<ProjectParameterDefinition> definitions = [];
        DefinitionBindingMapIterator iterator = document.ParameterBindings.ForwardIterator();
        while (iterator.MoveNext())
        {
            Definition definition = iterator.Key;
            if (iterator.Current is not ElementBinding binding) continue;
            if (TryCreateDefinition(document, definition, binding, out ProjectParameterDefinition? item))
                definitions.Add(item);
        }

        return new(definitions);
    }

    // A definition whose name cannot be read is skipped: an unreadable parameter is a document
    // problem of its own, not a duplicate to report.
    private static bool TryCreateDefinition(
        Document document,
        Definition definition,
        ElementBinding binding,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ProjectParameterDefinition? item)
    {
        item = null;
        try
        {
            string name = definition.Name;
            if (string.IsNullOrWhiteSpace(name)) return false;

            item = new ProjectParameterDefinition(
                name,
                ReadSharedGuid(document, definition),
                binding is InstanceBinding,
                ReadCategoryIds(binding));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // The definition of a user-defined parameter is backed by a ParameterElement with the same id,
    // and a shared parameter is the SharedParameterElement subclass: its GUID is the identity a
    // project parameter keeps across documents.
    private static Guid? ReadSharedGuid(Document document, Definition definition)
    {
        if (definition is not InternalDefinition internalDefinition) return null;
        if (internalDefinition.BuiltInParameter != BuiltInParameter.INVALID) return null;

        ElementId definitionId = internalDefinition.Id;
        if (definitionId == ElementId.InvalidElementId) return null;

        return document.GetElement(definitionId) is SharedParameterElement sharedParameter
            ? sharedParameter.GuidValue
            : null;
    }

    private static IReadOnlyList<ElementId> ReadCategoryIds(ElementBinding binding)
    {
        List<ElementId> categoryIds = [];
        foreach (Category category in binding.Categories)
        {
            if (category is null) continue;
            categoryIds.Add(category.Id);
        }

        return categoryIds;
    }

    private static Dictionary<string, Dictionary<ElementId, int>> BuildCategoryCounts(
        IReadOnlyList<ProjectParameterDefinition> definitions)
    {
        Dictionary<string, Dictionary<ElementId, int>> counts = new(StringComparer.Ordinal);
        foreach (ProjectParameterDefinition definition in definitions)
        {
            if (!counts.TryGetValue(definition.Name, out Dictionary<ElementId, int>? categories))
                counts[definition.Name] = categories = [];

            foreach (ElementId categoryId in definition.CategoryIds)
            {
                categories[categoryId] = categories.TryGetValue(categoryId, out int count)
                    ? count + 1
                    : 1;
            }
        }

        return counts;
    }
}
