using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the group that contains an element.
/// </summary>
public class GeneralGroupDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<MembersDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		Group? group = element.FindGroup();

		return group is null
			? []
			: [group];
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) => element.FindGroup();
}
