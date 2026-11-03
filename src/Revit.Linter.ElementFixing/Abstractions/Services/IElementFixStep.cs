using Autodesk.Revit.DB;

namespace Revit.Linter.ElementFixing.Abstractions.Services;

/// <summary>Executes one configured fix operation for a set of elements.</summary>
internal interface IElementFixStep
{
    /// <summary>Executes the step inside the transaction of the caller.</summary>
    /// <param name="document">The document that owns the elements.</param>
    /// <param name="elementIds">The distinct elements selected for the step by its element sets.</param>
    /// <returns><see langword="true"/> when every selected element was processed; otherwise <see langword="false"/>.</returns>
    bool Execute(Document document, IReadOnlyCollection<ElementId> elementIds);
}