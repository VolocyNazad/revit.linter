using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves rooms that contain an element.
/// </summary>
public class RoomDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<PlacedInsideRoomDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element) 
		=> element is ElementType ? [] : element.FindRooms();

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) 
		=> element is ElementType ? null : element.FindRoom();
}

