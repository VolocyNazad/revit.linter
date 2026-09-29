using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves internal insulation and lining elements attached to a host element.
/// </summary>
public class InternalInsulationDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<MEPCurveHostDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element) =>
		element is { } hostObject
			? hostObject.FindInternalInsulations()
			: [];

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element) =>
		element is { } hostObject
			? hostObject.FindInternalInsulations().FirstOrDefault()
			: null;
}
