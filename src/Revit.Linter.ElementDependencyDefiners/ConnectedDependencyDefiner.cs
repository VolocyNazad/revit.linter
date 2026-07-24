using Autodesk.Revit.DB;
using Revit.Linter.ElementDependencyDefiners.Abstractions;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;

namespace Revit.Linter.ElementDependencyDefiners;

public sealed class ConnectedDependencyDefiner : IElementsDependencyDefiner
{
public IElementsDependencyDefiner Inversed => DefinerInstance<ConnectedDependencyDefiner>.Value;

	public IEnumerable<Element> All(Element element) => element.FindConnected();
	public Element? FirstOrDefault(Element element)
	{
		Element? dependency = element.FindConnected().FirstOrDefault();

		return dependency;
	}
}
