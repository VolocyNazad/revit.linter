using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Позволяет определить экземпляры, для которых текущий элемент является типоразмером группы
/// </summary>
public class InstanceMembersDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<InstanceMembersDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element)
	{
		if (element is not ElementType elementType) return [];

		IEnumerable<Element> instances = elementType.FindInstances();
		return instances.OfType<Group>()
			.SelectMany(i => i.FindMembers());
	}

	public Element? FirstOrDefault(Element element) => All(element).FirstOrDefault();
}
