using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the members of a group.
/// </summary>
public class MembersDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<GeneralGroupDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		if (element is Group group) return group.FindMembers();
		return [];
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) => All(element).FirstOrDefault();
}
