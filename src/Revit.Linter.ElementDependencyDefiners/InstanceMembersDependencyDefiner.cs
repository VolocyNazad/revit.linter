using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the instances for which the current element is the group's type
/// </summary>
public class InstanceMembersDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<GeneralGroupTypeDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element)
	{
		if (element is not ElementType elementType) return [];

		IEnumerable<Element> instances = elementType.FindInstances();
		return instances.OfType<Group>()
			.SelectMany(i => i.FindMembers());
	}

	public Element? FirstOrDefault(Element element) => All(element).FirstOrDefault();
}
