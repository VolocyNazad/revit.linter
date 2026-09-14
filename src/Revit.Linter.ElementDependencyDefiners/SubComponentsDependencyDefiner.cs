using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the instances nested within the current instance
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
