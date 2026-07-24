using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Позволяет определить группу, в которой расположен текущий экземпляр
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
