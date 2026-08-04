using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

public class ExceptDependencyDefiner(IElementsDependencyDefiner first, IElementsDependencyDefiner second) : IElementsDependencyDefiner
{
	public IElementsDependencyDefiner? Inversed => null;

	public IEnumerable<Element> All(Element element)
	{
		HashSet<Element> firstSet = new(first.All(element), ElementEqualityComparer.Instance);
		firstSet.ExceptWith(second.All(element));
		return firstSet;
	}

	public Element? FirstOrDefault(Element element)
	{
		HashSet<Element> secondSet = new(second.All(element), ElementEqualityComparer.Instance);

		return first.All(element).FirstOrDefault(item => !secondSet.Contains(item));
	}
}

public class IntersectDependencyDefiner(IElementsDependencyDefiner first, IElementsDependencyDefiner second) : IElementsDependencyDefiner
{
	public IElementsDependencyDefiner? Inversed => null;

	public IEnumerable<Element> All(Element element)
	{
		HashSet<Element> firstSet = new(first.All(element), ElementEqualityComparer.Instance);
		firstSet.IntersectWith(second.All(element));
		return firstSet;
	}

	public Element? FirstOrDefault(Element element)
	{
		HashSet<Element> secondSet = new(second.All(element), ElementEqualityComparer.Instance);

		return first.All(element).FirstOrDefault(item => secondSet.Contains(item));
	}
}

public class UnionDependencyDefiner(IElementsDependencyDefiner first, IElementsDependencyDefiner second) : IElementsDependencyDefiner
{
	public IElementsDependencyDefiner? Inversed => null;

	public IEnumerable<Element> All(Element element)
	{
		var firstSet = new HashSet<Element>(first.All(element), ElementEqualityComparer.Instance);
		firstSet.UnionWith(second.All(element));
		return firstSet;
	}

	public Element? FirstOrDefault(Element element)
		=> first.FirstOrDefault(element)
		?? second.FirstOrDefault(element);
}

public class WithElementFilterDependencyDefiner(IElementsDependencyDefiner definer, ElementFilter elementFilter) : IElementsDependencyDefiner
{
	public IElementsDependencyDefiner? Inversed => null;

	public IEnumerable<Element> All(Element element)
		=> definer.All(element).Where(elementFilter.PassesFilter);

	public Element? FirstOrDefault(Element element)
		=> definer.All(element).FirstOrDefault(elementFilter.PassesFilter);
}

public class ElementFilterDependencyDefiner(ElementFilter elementFilter) : IElementsDependencyDefiner
{
	public IElementsDependencyDefiner? Inversed => null;

	public IEnumerable<Element> All(Element element)
	{
		Document doc = element.Document;
		return new FilteredElementCollector(doc).WherePasses(elementFilter).ToElements();
	}

	public Element? FirstOrDefault(Element element)
	{
		Document doc = element.Document;
		return new FilteredElementCollector(doc).WherePasses(elementFilter).FirstElement();
	}
}

public class EmptyDependencyDefiner : IElementsDependencyDefiner
{
	public IElementsDependencyDefiner? Inversed => null;

	public IEnumerable<Element> All(Element element) => [];

	public Element? FirstOrDefault(Element element) => null;
}

