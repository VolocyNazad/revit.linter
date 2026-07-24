using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Позволяет определить экземпляры, которые принадлежат семейству
/// </summary>
public class InstancesByFamilyDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<FamilyDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element)
	{
		Document document = element.Document;
		if (element is Family family)
			return family.GetFamilySymbolIds().Select(document.GetElement).OfType<FamilySymbol>().SelectMany(i => i.FindInstances());
		return [];
	}

	public Element? FirstOrDefault(Element element) => All(element).FirstOrDefault();
}
