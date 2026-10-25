using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.Presentation.ViewModels;

/// <summary>
/// Maintains the Revit event subscription state of view models that react to document and view changes.
/// </summary>
/// <remarks>
/// The add-in host owns the Revit subscription and publishes the events through
/// <see cref="IRevitEventSource"/>. A view model only observes that source while it is initialized, so it
/// never needs a Revit API context to subscribe and its reaction can be tested without Revit.
/// </remarks>
[XamlConstructor]
public abstract partial class RevitInteractionViewModel : InitializableObservableObject
{
    private readonly IRevitEventSource _revitEventSource;

    /// <summary>
    /// Initializes the view model with the source of the Revit application events it observes.
    /// </summary>
    /// <param name="revitEventSource">Publishes the Revit application events the view model reacts to.</param>
    protected RevitInteractionViewModel(IRevitEventSource revitEventSource)
    {
        _revitEventSource = revitEventSource;
    }

    /// <inheritdoc />
    protected override Task OnInitializing(CancellationToken cancellationToken = default)
    {
        _revitEventSource.Changed += OnRevitEventSourceChanged;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task OnDeinitializing(CancellationToken cancellationToken = default)
    {
        _revitEventSource.Changed -= OnRevitEventSourceChanged;
        return Task.CompletedTask;
    }

    private void OnRevitEventSourceChanged(RevitEventType revitEventType) => OnRevitChanged(revitEventType);

    /// <summary>
    /// Responds to a subscribed Revit application event.
    /// </summary>
    /// <param name="revitEventType">The kind of Revit change that occurred.</param>
    /// <remarks>The host raises the event on the Revit thread, once for every initialized view model.</remarks>
    protected abstract void OnRevitChanged(RevitEventType revitEventType);
}
