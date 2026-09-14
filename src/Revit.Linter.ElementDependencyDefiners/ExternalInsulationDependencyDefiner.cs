using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the external insulation instance for the current instance
/// </summary>
public class ExternalInsulationDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<MEPCurveHostDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element) =>
		element is { } hostObject
			? hostObject.FindExternalInsulations()
			: [];

	public Element? FirstOrDefault(Element element) =>
		element is { } hostObject
			? hostObject.FindExternalInsulations().FirstOrDefault()
			: null;
}
