using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Позволяет определить экземпляр, на основе которого размещен текущий экземпляр
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
