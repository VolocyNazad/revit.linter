using Revit.Linter.ElementChangesProvider.Abstractions.Models;

namespace Revit.Linter.ElementChangesProvider.Abstractions.Services;

public interface IElementChangesSender
{
    void Send(ElementChanges changes);
}
