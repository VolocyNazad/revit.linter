using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI.Events;
using Revit.Async;
using Revit.Events.Abstractions.Services;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels.Base;

[XamlConstructor, AutoConstructor]
internal abstract partial class RevitInteractionViewModel : InitializableObservableObject
{
    protected readonly IAsyncExternalEvent _externalEvent;

    protected override async Task OnInitializing(CancellationToken cancellationToken = default)
    {
        await RevitTask.RunAsync(uiapp => {
            uiapp.ViewActivated += ViewActivated;
            uiapp.ViewActivated += DocumentFocusChanged;
            var app = uiapp.Application;
            app.DocumentClosed += DocumentClosed;
            app.DocumentOpened += DocumentOpened;
            app.DocumentCreated += DocumentCreated;
            app.DocumentChanged += DocumentChanged;
            app.FamilyLoadedIntoDocument += FamilyLoadedIntoDocument;
        });

    }
    protected override async Task OnDeinitializing(CancellationToken cancellationToken = default)
    {
        await RevitTask.RunAsync(uiapp => {
            uiapp.ViewActivated -= ViewActivated;
            uiapp.ViewActivated -= DocumentFocusChanged;
            var app = uiapp.Application;
            app.DocumentClosed -= DocumentClosed;
            app.DocumentOpened -= DocumentOpened;
            app.DocumentCreated -= DocumentCreated;
            app.DocumentChanged -= DocumentChanged;
            app.FamilyLoadedIntoDocument -= FamilyLoadedIntoDocument;
        });
    }

    private async void FamilyLoadedIntoDocument(object? sender, FamilyLoadedIntoDocumentEventArgs e) => OnRevitChanged();
    private async void DocumentChanged(object? sender, DocumentChangedEventArgs e) => OnRevitChanged();
    private async void ViewActivated(object? sender, ViewActivatedEventArgs e) => OnRevitChanged();
    private async void DocumentCreated(object? sender, DocumentCreatedEventArgs e) => OnRevitChanged();
    private async void DocumentOpened(object? sender, DocumentOpenedEventArgs e) => OnRevitChanged();
    private async void DocumentClosed(object? sender, DocumentClosedEventArgs e) => OnRevitChanged();
    private async void DocumentFocusChanged(object? sender, ViewActivatedEventArgs e)
    {
        if (e.CurrentActiveView is null || e.PreviousActiveView is null) return;
        if (e.CurrentActiveView.Document.Equals(e.PreviousActiveView.Document)) return;
        OnRevitChanged();
    }

    protected abstract void OnRevitChanged();
}
