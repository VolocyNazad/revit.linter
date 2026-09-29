using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the type of the host of a family instance.
/// </summary>
public class HostTypeDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<InstanceInsertsDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element)
	{
		ICollection<Element> elements = [];
		if (element is not FamilyInstance familyInstance) return elements;

		Element host = familyInstance.Host;
		Element? type = host?.FindElementType();
		if (type != null)
			elements.Add(type);

		return elements;
	}

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) =>
		element is not FamilyInstance familyInstance 
			? null 
			: familyInstance.Host?.FindElementType();
}
