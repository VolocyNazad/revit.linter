using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves family instances placed inside a room.
/// </summary>
public class PlacedInsideRoomDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<RoomDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		IEnumerable<Element> placed = element is Room room
			? room.FindPlaced<FamilyInstance>()
			: [];
		return placed;
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) => element is Room room
			? room.FindPlaced<FamilyInstance>().FirstOrDefault()
			: null;
}
