using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Позволяет определить экземпляр помещения, в котором расположен текущий экземпляр
/// </summary>
public class RoomDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<PlacedInsideRoomDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element) 
		=> element is ElementType ? [] : element.FindRooms();

	public Element? FirstOrDefault(Element element) 
		=> element is ElementType ? null : element.FindRoom();
}

