namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>
/// Publishes the Revit application events that interaction view models react to.
/// </summary>
/// <remarks>
/// The add-in host owns the single Revit subscription that raises the event and does so on the Revit
/// thread. Consumers only add and remove handlers; they never subscribe to Revit events themselves,
/// so one event reaches every view model in one place and a view model can be tested without Revit.
/// </remarks>
public interface IRevitEventSource
{
    /// <summary>Occurs when Revit raises one of the observed application events.</summary>
    event Action<RevitEventType>? Changed;
}
