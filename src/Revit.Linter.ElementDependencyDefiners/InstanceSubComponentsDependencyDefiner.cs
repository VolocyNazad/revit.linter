using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Позволяет определить экземпляры, для которых текущий элемент является типоразмером родителя
/// </summary>
public class InstanceSubComponentsDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<GeneralSuperComponentTypeDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element)
	{
		if (element is not ElementType elementType) return [];

		List<Element> result = [];
		foreach (Element instance in elementType.FindInstances()) {
			if (instance is not FamilyInstance familyInstance) continue;

			result.AddRange(familyInstance.GetSubComponents());
		}

		return result;
	}

	public Element? FirstOrDefault(Element element) => All(element).FirstOrDefault();
}
