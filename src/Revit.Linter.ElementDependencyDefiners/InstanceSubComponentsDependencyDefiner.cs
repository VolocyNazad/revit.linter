using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves components nested within family instances of the specified type.
/// </summary>
public class InstanceSubComponentsDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<GeneralSuperComponentTypeDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		if (element is not ElementType elementType) return [];

		List<Element> result = [];
		foreach (Element instance in elementType.FindInstances()) {
			if (instance is not FamilyInstance familyInstance) continue;

			result.AddRange(familyInstance.GetSubComponents());
		}

		return result;
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) => All(element).FirstOrDefault();
}
