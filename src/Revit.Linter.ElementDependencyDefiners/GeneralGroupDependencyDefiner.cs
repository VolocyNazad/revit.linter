using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the group the current instance is located in
/// </summary>
public class GeneralGroupDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<MembersDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element)
	{
		Group? group = element.FindGroup();

		return group is null
			? []
			: [group];
	}

	public Element? FirstOrDefault(Element element) => element.FindGroup();
}
