using Autodesk.Revit.DB;

namespace Revit.Linter.ElementChangesProvider.Abstractions.Models;

/// <summary>
/// Отчет об измененных элементах
/// </summary>
/// <param name="Document"> Документ </param>
/// <param name="Creared"> Созданные </param>
/// <param name="Modified"> Измененные </param>
/// <param name="Deleted"> Удаленные </param>
public sealed record ElementChanges(Document Document, IEnumerable<ElementId> Creared, IEnumerable<ElementId> Modified, IEnumerable<ElementId> Deleted) { }
