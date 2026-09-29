using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves spaces that contain an element.
/// </summary>
public class SpaceDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<PlacedInsideSpaceDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element) 
		=> element is ElementType ? [] : element.FindSpaces();

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) 
		=> element is ElementType ? null : element.FindSpace();
}
