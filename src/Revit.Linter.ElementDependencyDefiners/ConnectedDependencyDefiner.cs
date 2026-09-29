using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

/// <summary>
/// Resolves elements connected to an element through MEP connectors.
/// </summary>
public sealed class ConnectedDependencyDefiner : IElementsDependencyDefiner
{
/// <inheritdoc />
public IElementsDependencyDefiner Inversed => DefinerInstance<ConnectedDependencyDefiner>.Value;

	/// <inheritdoc />
	public IEnumerable<Element> All(Element element) => element.FindConnected();

	/// <inheritdoc />
	public Element? FirstOrDefault(Element element)
	{
		Element? dependency = element.FindConnected().FirstOrDefault();

		return dependency;
	}
}
