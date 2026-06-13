using Revit.Linter.ElementAccentor.Abstractions.Models;

namespace Revit.Linter.ElementAccentor.Abstractions.Services;

public interface IAccentElementsService
{
    AccentElementsType Type { get; }
    bool Execute(Document document, params ElementId[] elementIds);
}
