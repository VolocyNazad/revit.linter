using Autodesk.Revit.DB;

namespace Revit.Linter.ElementChangesProvider.Abstractions.Models;

/// <summary>
/// Report of changed elements
/// </summary>
/// <param name="Document"> Document </param>
/// <param name="Creared"> Created </param>
/// <param name="Modified"> Modified </param>
/// <param name="Deleted"> Deleted </param>
public sealed record ElementChanges(Document Document, IEnumerable<ElementId> Creared, IEnumerable<ElementId> Modified, IEnumerable<ElementId> Deleted) { }
