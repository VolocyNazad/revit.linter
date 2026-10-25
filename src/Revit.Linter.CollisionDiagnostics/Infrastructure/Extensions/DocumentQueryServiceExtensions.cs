using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;

internal static class DocumentQueryServiceExtensions
{
    // Separates the parts of a composite cache argument; it cannot occur in a formula or a group value
    // typed by a user, so two different part lists never produce the same argument.
    private const string ArgumentSeparator = "\u001F";

    // Guards the bounding-box path: elements without geometry are skipped upstream (they yield
    // no solids and stay out of the collision groups), so a missing geometry here means
    // the rule selected an element it cannot visualize or compare, and the rule must fail loudly.
    public static GeometryElement GetRequiredGeometry(
        this IDocumentQueryService documentQueries, Element element, View? view)
        => documentQueries.GetGeometry(element, view)
           ?? throw new InvalidOperationException($"Element {element.Id} has no geometry.");

    /// <summary>Gets a cached collision query, creating it on a miss.</summary>
    /// <param name="documentQueries">The document query service.</param>
    /// <param name="document">The queried document.</param>
    /// <param name="view">The view the query is limited to, or <see langword="null"/> for the whole document.</param>
    /// <param name="query">The collision query to cache.</param>
    /// <param name="compute">Creates the value on a cache miss.</param>
    /// <param name="argumentParts">Values that distinguish queries sharing a name, joined into one cache argument.</param>
    /// <returns>The cached or newly created value.</returns>
    /// <remarks>
    /// The keys carry the rule formulas rather than the rule code: rules with the same formulas share
    /// the collected elements and groups, and rules with different formulas never read each other's.
    /// </remarks>
    public static T GetCached<T>(
        this IDocumentQueryService documentQueries,
        Document document,
        View? view,
        CollisionQuery query,
        Func<T> compute,
        params string[] argumentParts)
    {
        string? argument = argumentParts.Length == 0
            ? null
            : string.Join(ArgumentSeparator, argumentParts);
        return documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, query.CacheName(), view, argument: argument), compute);
    }
}
