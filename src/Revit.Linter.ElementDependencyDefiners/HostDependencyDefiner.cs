using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the host of a family instance.
/// </summary>
public class HostDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<InsertsDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		ICollection<Element> elements = [];
		if (element is not FamilyInstance familyInstance) return elements;

		Element host = familyInstance.Host;
		if (host != null) elements.Add(host);
		return elements;
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) =>
		element is FamilyInstance familyInstance
			? familyInstance.Host
			: null;
}
