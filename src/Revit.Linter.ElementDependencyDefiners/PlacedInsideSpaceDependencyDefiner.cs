using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

public class PlacedInsideSpaceDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<SpaceDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element) => element is Space space
			? space.FindPlaced<FamilyInstance>()
			: [];

	public Element? FirstOrDefault(Element element) => element is Space space
			? space.FindPlaced<FamilyInstance>().FirstOrDefault()
			: null;
}
