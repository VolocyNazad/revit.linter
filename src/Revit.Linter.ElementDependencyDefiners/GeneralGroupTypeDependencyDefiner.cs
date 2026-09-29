using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the type of the group that contains an element.
/// </summary>
public class GeneralGroupTypeDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<InstanceMembersDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		Element? groupType = element.FindGroup()?.FindElementType();

		return groupType is null
			? []
			: [groupType];
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) => element.FindGroup()?.FindElementType();
}
