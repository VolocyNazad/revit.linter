using Revit.Linter.ElementChangesProvider.Abstractions.Models;
using Revit.Linter.ElementChangesProvider.Abstractions.Services;

namespace Revit.Linter.ElementChangesProvider.Services;

internal sealed class ElementChangesProvider : IElementChangesReceiver, IElementChangesSender
{
    public event ElementChangesHandler? Sent;

    public void Send(ElementChanges changes) => Sent?.Invoke(this, new(changes));
}
