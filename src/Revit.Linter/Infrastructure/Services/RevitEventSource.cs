using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.Infrastructure.Services;

/// <summary>
/// Publishes the Revit application events the add-in host observes on behalf of interaction view models.
/// </summary>
/// <remarks>
/// The host's Revit event handlers call <see cref="Raise"/>; view models only subscribe to
/// <see cref="Changed"/>. The class holds no Revit API dependency, so its fan-out can be reasoned about
/// without a live Revit process.
/// </remarks>
internal sealed class RevitEventSource : IRevitEventSource
{
    /// <inheritdoc />
    public event Action<RevitEventType>? Changed;

    /// <summary>Notifies every subscribed view model about one observed Revit event.</summary>
    /// <param name="revitEventType">The kind of Revit change that occurred.</param>
    public void Raise(RevitEventType revitEventType) => Changed?.Invoke(revitEventType);
}
