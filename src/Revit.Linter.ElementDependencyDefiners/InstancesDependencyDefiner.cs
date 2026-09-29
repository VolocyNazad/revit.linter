using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the instances that belong to an element type.
/// </summary>
public class InstancesDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<TypeDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
		=> element is ElementType elementType ? elementType.FindInstances() : [];

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element)
	{
		return element is ElementType elementType
			? elementType.FindInstances().FirstOrDefault()
			: null;
	}
}
