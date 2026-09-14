using Autodesk.Revit.DB;

namespace Revit.Linter.ElementDependencyDefiners.Infrastructure;

internal sealed class ElementEqualityComparer : IEqualityComparer<Element>
{
	public static ElementEqualityComparer Instance { get; } = new();

	// Document does not override Equals, and different accesses to the same element are not
	// guaranteed to return the same managed object - compare by Id only.
	public bool Equals(Element? x, Element? y)
		=> ReferenceEquals(x, y)
		|| x is not null
		&& y is not null
		&& x.Id == y.Id;

	public int GetHashCode(Element element)
		=> element.Id.GetHashCode();
}

