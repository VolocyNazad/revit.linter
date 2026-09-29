using Autodesk.Revit.DB;

namespace Revit.Linter.ElementChangesProvider.Abstractions.Models;

/// <summary>
/// Describes element changes reported for a Revit document.
/// </summary>
/// <param name="Document">The document whose elements changed.</param>
/// <param name="Creared">The identifiers of elements that were created.</param>
/// <param name="Modified">The identifiers of elements that were modified.</param>
/// <param name="Deleted">The identifiers of elements that were deleted.</param>
public sealed record ElementChanges(Document Document, IEnumerable<ElementId> Creared, IEnumerable<ElementId> Modified, IEnumerable<ElementId> Deleted) { }
