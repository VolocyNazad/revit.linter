using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the arbitrary scope box that intersects the instance
/// </summary>
public class ScopeBoxDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<InstancesInsideScopeBoxDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element) 
		=> element is ElementType ? [] : element.FindScopeBoxes();

	public Element? FirstOrDefault(Element element) 
		=> element is ElementType ? null : element.FindScopeBox();
}
