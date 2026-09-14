using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the instances for which the current instance is the host
/// </summary>
public class InsertsDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<HostDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element) =>
		element switch {
			HostObject hostObject => hostObject.FindInserted(),
			FamilyInstance familyInstance => familyInstance.FindInserted(),
			_ => []
		};


	public Element? FirstOrDefault(Element element) =>
		element switch {
			HostObject hostObject => hostObject.FindInserted().FirstOrDefault(),
			FamilyInstance familyInstance => familyInstance.FindInserted().FirstOrDefault(),
			_ => null
		};
}
