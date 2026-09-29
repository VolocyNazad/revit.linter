using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves elements inserted into a host object or family instance.
/// </summary>
public class InsertsDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<HostDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element) =>
		element switch {
			HostObject hostObject => hostObject.FindInserted(),
			FamilyInstance familyInstance => familyInstance.FindInserted(),
			_ => []
		};


	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) =>
		element switch {
			HostObject hostObject => hostObject.FindInserted().FirstOrDefault(),
			FamilyInstance familyInstance => familyInstance.FindInserted().FirstOrDefault(),
			_ => null
		};
}
