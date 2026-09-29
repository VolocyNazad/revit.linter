using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves scope boxes that intersect an element.
/// </summary>
public class ScopeBoxDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<InstancesInsideScopeBoxDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element) 
		=> element is ElementType ? [] : element.FindScopeBoxes();

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) 
		=> element is ElementType ? null : element.FindScopeBox();
}
