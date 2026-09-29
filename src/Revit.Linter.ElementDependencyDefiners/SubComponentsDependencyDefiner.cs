using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the components nested within a family instance.
/// </summary>
public class SubComponentsDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<GeneralSuperComponentDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		IEnumerable<Element> result = element is FamilyInstance familyInstance
			? (IEnumerable<Element>)familyInstance.GetSubComponents()
			: [];
		return result;
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element)
	{
		if (element is FamilyInstance familyInstance) {
			return familyInstance.GetSubComponents().FirstOrDefault();
		}
		return null;
	}
}
