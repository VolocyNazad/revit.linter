using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the instances that belong to the type
/// </summary>
public class InstancesDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<TypeDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element)
		=> element is ElementType elementType ? elementType.FindInstances() : [];

	public Element? FirstOrDefault(Element element)
	{
		return element is ElementType elementType
			? elementType.FindInstances().FirstOrDefault()
			: null;
	}
}
