using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Позволяет определить типоразмер, на основе экземпляра которого размещен текущий экземпляр
/// </summary>
public class HostTypeDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<InstanceInsertsDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element)
	{
		ICollection<Element> elements = [];
		if (element is not FamilyInstance familyInstance) return elements;

		Element host = familyInstance.Host;
		Element? type = host?.FindElementType();
		if (type != null)
			elements.Add(type);

		return elements;
	}

	public Element? FirstOrDefault(Element element) =>
		element is not FamilyInstance familyInstance 
			? null 
			: familyInstance.Host?.FindElementType();
}
