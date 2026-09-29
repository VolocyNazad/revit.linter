using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI.Events;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.RunDiagnosticPresenter.ViewModels.Base;

/// <summary>
/// Maintains Revit event subscriptions for view models whose command availability depends on application state.
/// </summary>
[XamlConstructor]
public abstract partial class RevitInteractionViewModel : InitializableObservableObject
{
    /// <summary>
    /// Schedules subscription work in a valid Revit API context.
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

    private void FamilyLoadedIntoDocument(object? sender, FamilyLoadedIntoDocumentEventArgs e) => OnRevitChanged();
    private void DocumentChanged(object? sender, DocumentChangedEventArgs e) => OnRevitChanged();
    private void ViewActivated(object? sender, ViewActivatedEventArgs e) => OnRevitChanged();
    private void DocumentCreated(object? sender, DocumentCreatedEventArgs e) => OnRevitChanged();
    private void DocumentOpened(object? sender, DocumentOpenedEventArgs e) => OnRevitChanged();
    private void DocumentClosed(object? sender, DocumentClosedEventArgs e) => OnRevitChanged();
    private void DocumentFocusChanged(object? sender, ViewActivatedEventArgs e)
    {
        if (e.CurrentActiveView is null || e.PreviousActiveView is null) return;
        if (e.CurrentActiveView.Document.Equals(e.PreviousActiveView.Document)) return;
        OnRevitChanged();
    }

    /// <summary>
    /// Responds when a subscribed Revit application event may have changed presentation state.
    /// </summary>
    protected abstract void OnRevitChanged();
}
