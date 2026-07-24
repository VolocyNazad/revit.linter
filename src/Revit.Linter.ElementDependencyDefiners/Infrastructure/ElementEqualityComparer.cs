using Autodesk.Revit.DB;

namespace Revit.Linter.ElementDependencyDefiners.Infrastructure;

internal sealed class ElementEqualityComparer : IEqualityComparer<Element>
{
	public static ElementEqualityComparer Instance { get; } = new();

	public bool Equals(Element? x, Element? y)
		=> ReferenceEquals(x, y)
		|| x is not null
		&& y is not null
		&& ReferenceEquals(x.Document, y.Document)
		&& x.Id == y.Id;

	public int GetHashCode(Element element)
	{
		unchecked
		{
			return (element.Document.GetHashCode() * 397) ^ element.Id.GetHashCode();
		}
	}
}

