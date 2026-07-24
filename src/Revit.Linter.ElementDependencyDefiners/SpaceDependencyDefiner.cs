using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

public class SpaceDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<PlacedInsideSpaceDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element) 
		=> element is ElementType ? [] : element.FindSpaces();

	public Element? FirstOrDefault(Element element) 
		=> element is ElementType ? null : element.FindSpace();
}
