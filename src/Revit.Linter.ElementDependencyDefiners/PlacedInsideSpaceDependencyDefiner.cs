using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves family instances placed inside a space.
/// </summary>
public class PlacedInsideSpaceDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<SpaceDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element) => element is Space space
			? space.FindPlaced<FamilyInstance>()
			: [];

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) => element is Space space
			? space.FindPlaced<FamilyInstance>().FirstOrDefault()
			: null;
}
