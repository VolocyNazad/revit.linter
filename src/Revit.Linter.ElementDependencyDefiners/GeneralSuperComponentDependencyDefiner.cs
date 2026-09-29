using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the top-level parent family instance of a nested component.
/// </summary>
public class GeneralSuperComponentDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<SubComponentsDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		if (element is not FamilyInstance familyInstance) return [];

		Element? superComponent = familyInstance.GetSuperPuperComponent();
		return superComponent != null
			? [superComponent]
			: [];
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element)
	{
		if (element is FamilyInstance familyInstance)
			return familyInstance.GetSuperPuperComponent();
		return null;
	}
}
