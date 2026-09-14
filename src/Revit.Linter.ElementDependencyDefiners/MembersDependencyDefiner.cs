using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the instances for which the current element is the group
/// </summary>
public class MembersDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<GeneralGroupDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element)
	{
		if (element is Group group) return group.FindMembers();
		return [];
	}

	public Element? FirstOrDefault(Element element) => All(element).FirstOrDefault();
}
