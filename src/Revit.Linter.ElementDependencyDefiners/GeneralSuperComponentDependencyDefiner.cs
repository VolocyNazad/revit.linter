using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the parent family instance the current instance belongs to
/// </summary>
public class GeneralSuperComponentDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<SubComponentsDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element)
	{
		if (element is not FamilyInstance familyInstance) return [];

		Element? superComponent = familyInstance.GetSuperPuperComponent();
		return superComponent != null
			? [superComponent]
			: [];
	}

	public Element? FirstOrDefault(Element element)
	{
		if (element is FamilyInstance familyInstance)
			return familyInstance.GetSuperPuperComponent();
		return null;
	}
}
