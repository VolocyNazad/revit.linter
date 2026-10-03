namespace Revit.Linter.DocumentQueries.Abstractions.Models;

/// <summary>
/// Identifies one cached query against a Revit document.
/// </summary>
/// <remarks>
/// Two keys are equal when every component is equal, so the same query asked from different modules
/// resolves to one cache entry. The document is identified by its hash code and title rather than by
/// the <see cref="Document"/> instance, which keeps the key a plain value that stays comparable after
/// the document is closed. Element identifiers are unique only inside one document, so every key
/// carries the document identity.
/// </remarks>
public readonly record struct DocumentQueryKey
{
    private DocumentQueryKey(
        int documentHash,
        string documentTitle,
        string query,
        ElementId? viewId,
        ElementId? elementId,
        string? argument)
    {
        DocumentHash = documentHash;
        DocumentTitle = documentTitle;
        Query = query;
        ViewId = viewId;
        ElementId = elementId;
        Argument = argument;
    }

    /// <summary>
    /// Gets the hash code of the queried document.
    /// </summary>
    public int DocumentHash { get; }

    /// <summary>
    /// Gets the title of the queried document.
    /// </summary>
    public string DocumentTitle { get; }

    /// <summary>
    /// Gets the name of the query, for example <c>document:elements</c>.
    /// </summary>
    public string Query { get; }

    /// <summary>
    /// Gets the view the query is limited to, or <see langword="null"/> for a document-wide query.
    /// </summary>
    public ElementId? ViewId { get; }

    /// <summary>
    /// Gets the element the query is about, or <see langword="null"/> for a query about the document.
    /// </summary>
    public ElementId? ElementId { get; }

    /// <summary>
    /// Gets the additional value that distinguishes queries sharing a name, or <see langword="null"/>.
    /// </summary>
    public string? Argument { get; }

    /// <summary>
    /// Creates the key of a query against <paramref name="document"/>.
    /// </summary>
    /// <param name="document">The queried document.</param>
    /// <param name="query">The query name. Module-specific queries prefix it with the module name.</param>
    /// <param name="view">The view the query is limited to, or <see langword="null"/> for the whole document.</param>
    /// <param name="elementId">The element the query is about, when it is about a single element.</param>
    /// <param name="argument">An additional value that distinguishes queries sharing a name.</param>
    /// <returns>The key identifying the query in the transaction cache.</returns>
    public static DocumentQueryKey Create(
        Document document,
        string query,
        View? view = null,
        ElementId? elementId = null,
        string? argument = null)
        => new(document.GetHashCode(), document.Title, query, view?.Id, elementId, argument);
}