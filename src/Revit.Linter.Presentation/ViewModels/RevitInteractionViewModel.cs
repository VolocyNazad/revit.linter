using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI.Events;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.Presentation.ViewModels;

/// <summary>
/// Maintains Revit event subscriptions for view models that react to document and view changes.
/// </summary>
[XamlConstructor]
public abstract partial class RevitInteractionViewModel : InitializableObservableObject
{
    /// <summary>
    /// Schedules work in a valid Revit API context.
    /// </summary>
    protected readonly IRevitIdlingScheduler _idlingScheduler;

    /// <summary>
    /// Initializes the view model with a scheduler for Revit API work.
    /// </summary>
    /// <param name="idlingScheduler">Schedules work in a valid Revit API context.</param>
    protected RevitInteractionViewModel(IRevitIdlingScheduler idlingScheduler)
    {
        _idlingScheduler = idlingScheduler;
    }

    /// <inheritdoc />
    protected override async Task OnInitializing(CancellationToken cancellationToken = default)
    {
        await _idlingScheduler.RunAsync(uiapp => {
            uiapp.ViewActivated += ViewActivated;
            uiapp.ViewActivated += DocumentFocusChanged;
            var app = uiapp.Application;
            app.DocumentClosed += DocumentClosed;
            app.DocumentOpened += DocumentOpened;
            app.DocumentCreated += DocumentCreated;
            app.DocumentChanged += DocumentChanged;
            app.FamilyLoadedIntoDocument += FamilyLoadedIntoDocument;
        }, cancellationToken);

    }
    /// <inheritdoc />
    protected override async Task OnDeinitializing(CancellationToken cancellationToken = default)
    {
        await _idlingScheduler.RunAsync(uiapp => {
            uiapp.ViewActivated -= ViewActivated;
            uiapp.ViewActivated -= DocumentFocusChanged;
            var app = uiapp.Application;
            app.DocumentClosed -= DocumentClosed;
            app.DocumentOpened -= DocumentOpened;
            app.DocumentCreated -= DocumentCreated;
            app.DocumentChanged -= DocumentChanged;
            app.FamilyLoadedIntoDocument -= FamilyLoadedIntoDocument;
        }, cancellationToken);
    }

    private void FamilyLoadedIntoDocument(object? sender, FamilyLoadedIntoDocumentEventArgs e) => OnRevitChanged(RevitEventType.FamilyLoadedIntoDocument);
    private void DocumentChanged(object? sender, DocumentChangedEventArgs e) => OnRevitChanged(RevitEventType.DocumentChanged);
    private void ViewActivated(object? sender, ViewActivatedEventArgs e) => OnRevitChanged(RevitEventType.ViewActivated);
    private void DocumentCreated(object? sender, DocumentCreatedEventArgs e) => OnRevitChanged(RevitEventType.DocumentCreated);
    private void DocumentOpened(object? sender, DocumentOpenedEventArgs e) => OnRevitChanged(RevitEventType.DocumentOpened);
    private void DocumentClosed(object? sender, DocumentClosedEventArgs e) => OnRevitChanged(RevitEventType.DocumentClosed);
    private void DocumentFocusChanged(object? sender, ViewActivatedEventArgs e)
    {
        if (e.CurrentActiveView is null || e.PreviousActiveView is null) return;
        if (e.CurrentActiveView.Document.Equals(e.PreviousActiveView.Document)) return;
        OnRevitChanged(RevitEventType.DocumentFocusChanged);
    }

    /// <summary>
    /// Responds to a subscribed Revit application event.
    /// </summary>
    /// <param name="revitEventType">The kind of Revit change that occurred.</param>
    protected abstract void OnRevitChanged(RevitEventType revitEventType);
}
