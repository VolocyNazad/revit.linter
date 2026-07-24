using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Позволяет определить типоразмер для текущего элемента
/// </summary>
public class TypeDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<InstancesDependencyDefiner>.Value;
	public IEnumerable<Element> All(Element element)
	{
		ElementType? target = element.FindElementType();
		return target is null
			? []
			: [target];
	}
	public Element? FirstOrDefault(Element element) => element.FindElementType();
}

/// <summary>
/// Позволяет определить семейство для текущего элемента
/// </summary>
public class FamilyDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<InstancesByFamilyDependencyDefiner>.Value;
	public IEnumerable<Element> All(Element element)
	{
		Element? target = element.FindElementType();
		if (target is not FamilySymbol familySymbol) return [];
		target = familySymbol.Family;
		return [target];
	}
	public Element? FirstOrDefault(Element element)
	{
		Element? target = element.FindElementType();
		if (target is not FamilySymbol familySymbol) return null;
		target = familySymbol.Family;
		return target;
	}
}
