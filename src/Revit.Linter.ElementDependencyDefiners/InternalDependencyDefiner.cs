using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves the source element itself.
/// </summary>
public class InternalDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<InternalDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element) 
		=> [element];

	/// <inheritdoc />
	public Element FirstOrDefault(Element element) => element;
}
