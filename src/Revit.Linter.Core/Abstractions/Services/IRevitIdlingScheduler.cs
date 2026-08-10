using Autodesk.Revit.UI;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Schedules work for the next Revit Idling event.</summary>
public interface IRevitIdlingScheduler
{
    Task RunAsync(Action<UIApplication> action, CancellationToken cancellationToken = default);
}
