using Autodesk.Revit.DB;

namespace Revit.Linter.ElementDependencyDefiners.Infrastructure;

internal sealed class ElementEqualityComparer : IEqualityComparer<Element>
{
	public static ElementEqualityComparer Instance { get; } = new();

	// Document не переопределяет Equals, а разные обращения к одному и тому же элементу не
	// гарантируют один и тот же управляемый объект - сравниваем только по Id.
	public bool Equals(Element? x, Element? y)
		=> ReferenceEquals(x, y)
		|| x is not null
		&& y is not null
		&& x.Id == y.Id;

	public int GetHashCode(Element element)
		=> element.Id.GetHashCode();
}

