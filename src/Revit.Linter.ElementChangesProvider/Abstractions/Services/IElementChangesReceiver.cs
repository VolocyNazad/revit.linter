using Revit.Linter.ElementChangesProvider.Abstractions.Models;

namespace Revit.Linter.ElementChangesProvider.Abstractions.Services;

public interface IElementChangesReceiver
{
    public event ElementChangesHandler? Sent;
}
