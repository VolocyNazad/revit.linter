using Autodesk.Revit.UI;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Schedules work for the next Revit Idling event.</summary>
public interface IRevitIdlingScheduler
{
    /// <summary>Runs an action in a valid Revit API context during a subsequent Idling event.</summary>
    /// <param name="action">The action that receives the active Revit application.</param>
    /// <param name="cancellationToken">Cancels work that has not completed.</param>
    /// <returns>A task that completes after the action finishes.</returns>
    Task RunAsync(Action<UIApplication> action, CancellationToken cancellationToken = default);
}
