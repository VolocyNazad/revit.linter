using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Позволяет определить экземпляры, которые являются вложенными для текущего экземпляра
/// </summary>
public class SubComponentsDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<GeneralSuperComponentDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element)
	{
		IEnumerable<Element> result = element is FamilyInstance familyInstance
			? (IEnumerable<Element>)familyInstance.GetSubComponents()
			: [];
		return result;
	}

	public Element? FirstOrDefault(Element element)
	{
		if (element is FamilyInstance familyInstance) {
			return familyInstance.GetSubComponents().FirstOrDefault();
		}
		return null;
	}
}
