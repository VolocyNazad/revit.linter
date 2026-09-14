using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the instances placed inside the current room
/// </summary>
public class PlacedInsideRoomDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<RoomDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element)
	{
		IEnumerable<Element> placed = element is Room room
			? room.FindPlaced<FamilyInstance>()
			: [];
		return placed;
	}

	public Element? FirstOrDefault(Element element) => element is Room room
			? room.FindPlaced<FamilyInstance>().FirstOrDefault()
			: null;
}
