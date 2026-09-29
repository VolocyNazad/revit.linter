using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the type of an element.
/// </summary>
public class TypeDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<InstancesDependencyDefiner>.Value;
	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		ElementType? target = element.FindElementType();
		return target is null
			? []
			: [target];
	}
	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) => element.FindElementType();
}

/// <summary>
/// Resolves the family of an element.
/// </summary>
public class FamilyDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<InstancesByFamilyDependencyDefiner>.Value;
	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		Element? target = element.FindElementType();
		if (target is not FamilySymbol familySymbol) return [];
		target = familySymbol.Family;
		return [target];
	}
	/// <inheritdoc />
	public Element? FirstOrDefault(Element element)
	{
		Element? target = element.FindElementType();
		if (target is not FamilySymbol familySymbol) return null;
		target = familySymbol.Family;
		return target;
	}
}
