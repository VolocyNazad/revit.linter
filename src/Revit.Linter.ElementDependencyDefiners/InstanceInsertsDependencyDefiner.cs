using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Позволяет определить экземпляры, для которых текущий элемент является типоразмером основы
/// </summary>
public class InstanceInsertsDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<HostTypeDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element)
	{
		if (element is not ElementType elementType) return [];

		IEnumerable<Element> instances = elementType.FindInstances();

		return instances
			.SelectMany(i => i switch {
				HostObject hostObject => hostObject.FindInserted(),
				FamilyInstance familyInstance => familyInstance.FindInserted(),
				_ => []
			});
	}

	public Element? FirstOrDefault(Element element) => All(element).FirstOrDefault();
}
