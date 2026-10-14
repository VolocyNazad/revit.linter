namespace Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;

internal interface IGetConnectedElementIdsService
{
    /// <summary>
    /// Returns the identifiers of the elements joined to <paramref name="element"/> by MEP connectors.
    /// </summary>
    /// <remarks>
    /// The result is cached per document: connector references do not depend on the view.
    /// The returned set is owned by the cache and must not be modified.
    /// </remarks>
    HashSet<long> Execute(Element element);
}
