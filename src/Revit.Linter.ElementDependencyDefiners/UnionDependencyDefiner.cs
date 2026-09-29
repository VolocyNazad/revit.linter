using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves elements produced by the first definer but not by the second definer.
/// </summary>
/// <param name="first">The definer that supplies the candidate elements.</param>
/// <param name="second">The definer whose elements are excluded.</param>
public class ExceptDependencyDefiner(IElementsDependencyDefiner first, IElementsDependencyDefiner second) : IElementsDependencyDefiner
{
	/// <inheritdoc />
	public IElementsDependencyDefiner? Inversed => null;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		HashSet<Element> firstSet = new(first.All(element), ElementEqualityComparer.Instance);
		firstSet.ExceptWith(second.All(element));
		return firstSet;
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element)
	{
		HashSet<Element> secondSet = new(second.All(element), ElementEqualityComparer.Instance);

		return first.All(element).FirstOrDefault(item => !secondSet.Contains(item));
	}
}

/// <summary>
/// Resolves elements produced by both supplied definers.
/// </summary>
/// <param name="first">The first dependency definer.</param>
/// <param name="second">The second dependency definer.</param>
public class IntersectDependencyDefiner(IElementsDependencyDefiner first, IElementsDependencyDefiner second) : IElementsDependencyDefiner
{
	/// <inheritdoc />
	public IElementsDependencyDefiner? Inversed => null;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		HashSet<Element> firstSet = new(first.All(element), ElementEqualityComparer.Instance);
		firstSet.IntersectWith(second.All(element));
		return firstSet;
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element)
	{
		HashSet<Element> secondSet = new(second.All(element), ElementEqualityComparer.Instance);

		return first.All(element).FirstOrDefault(item => secondSet.Contains(item));
	}
}

/// <summary>
/// Resolves the distinct union of elements produced by two definers.
/// </summary>
/// <param name="first">The first dependency definer.</param>
/// <param name="second">The second dependency definer.</param>
public class UnionDependencyDefiner(IElementsDependencyDefiner first, IElementsDependencyDefiner second) : IElementsDependencyDefiner
{
	/// <inheritdoc />
	public IElementsDependencyDefiner? Inversed => null;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		var firstSet = new HashSet<Element>(first.All(element), ElementEqualityComparer.Instance);
		firstSet.UnionWith(second.All(element));
		return firstSet;
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element)
		=> first.FirstOrDefault(element)
		?? second.FirstOrDefault(element);
}

/// <summary>
/// Filters the elements produced by another dependency definer.
/// </summary>
/// <param name="definer">The dependency definer whose results are filtered.</param>
/// <param name="elementFilter">The Revit element filter applied to the results.</param>
public class WithElementFilterDependencyDefiner(IElementsDependencyDefiner definer, ElementFilter elementFilter) : IElementsDependencyDefiner
{
	/// <inheritdoc />
	public IElementsDependencyDefiner? Inversed => null;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
		=> definer.All(element).Where(elementFilter.PassesFilter);

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element)
		=> definer.All(element).FirstOrDefault(elementFilter.PassesFilter);
}

/// <summary>
/// Resolves document elements accepted by a Revit element filter.
/// </summary>
/// <param name="elementFilter">The Revit element filter applied to the source element's document.</param>
public class ElementFilterDependencyDefiner(ElementFilter elementFilter) : IElementsDependencyDefiner
{
	/// <inheritdoc />
	public IElementsDependencyDefiner? Inversed => null;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		Document doc = element.Document;
		return new FilteredElementCollector(doc).WherePasses(elementFilter).ToElements();
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element)
	{
		Document doc = element.Document;
		return new FilteredElementCollector(doc).WherePasses(elementFilter).FirstElement();
	}
}

/// <summary>
/// Represents a dependency relationship that never resolves an element.
/// </summary>
public class EmptyDependencyDefiner : IElementsDependencyDefiner
{
	/// <inheritdoc />
	public IElementsDependencyDefiner? Inversed => null;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element) => [];

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) => null;
}

