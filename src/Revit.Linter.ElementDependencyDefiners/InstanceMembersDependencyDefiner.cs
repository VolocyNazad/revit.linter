using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves members of groups that have the specified group type.
/// </summary>
public class InstanceMembersDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<GeneralGroupTypeDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		if (element is not ElementType elementType) return [];

		IEnumerable<Element> instances = elementType.FindInstances();
		return instances.OfType<Group>()
			.SelectMany(i => i.FindMembers());
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) => All(element).FirstOrDefault();
}
