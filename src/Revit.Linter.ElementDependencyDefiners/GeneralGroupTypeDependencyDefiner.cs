using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Позволяет определить типоразмер группы, в которой расположен текущий экземпляр
/// </summary>
public class GeneralGroupTypeDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<InstanceMembersDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element)
	{
		Element? groupType = element.FindGroup()?.FindElementType();

		return groupType is null
			? []
			: [groupType];
	}

	public Element? FirstOrDefault(Element element) => element.FindGroup()?.FindElementType();
}
