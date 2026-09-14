using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the instance the current instance is hosted on
/// </summary>
public class HostDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<InsertsDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element)
	{
		ICollection<Element> elements = [];
		if (element is not FamilyInstance familyInstance) return elements;

		Element host = familyInstance.Host;
		if (host != null) elements.Add(host);
		return elements;
	}

	public Element? FirstOrDefault(Element element) =>
		element is FamilyInstance familyInstance
			? familyInstance.Host
			: null;
}
