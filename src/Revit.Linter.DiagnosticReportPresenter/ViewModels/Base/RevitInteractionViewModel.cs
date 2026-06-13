using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI.Events;
using Revit.Context.Abstractions.Services;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels.Base;

[XamlConstructor, AutoConstructor]
internal abstract partial class RevitInteractionViewModel : InitializableObservableObject
{
    protected readonly IRevitContext _revitContext;

    protected override async Task OnInitializing(CancellationToken cancellationToken = default)
    {
        Application app = _revitContext.Application!;
        app.DocumentClosed += DocumentClosed;
        app.DocumentOpened += DocumentOpened;
        app.DocumentCreated += DocumentCreated;
        app.DocumentChanged += DocumentChanged;
        app.FamilyLoadedIntoDocument += FamilyLoadedIntoDocument;
        UIApplication uiapp = _revitContext.UIApplication!;
        uiapp.ViewActivated += ViewActivated;
        uiapp.ViewActivated += DocumentFocusChanged;
    }
    protected override async Task OnDeinitializing(CancellationToken cancellationToken = default)
    {
        Application app = _revitContext.Application!;
        app.DocumentClosed -= DocumentClosed;
        app.DocumentOpened -= DocumentOpened;
        app.DocumentCreated -= DocumentCreated;
        app.DocumentChanged -= DocumentChanged;
        app.FamilyLoadedIntoDocument -= FamilyLoadedIntoDocument;
        UIApplication uiapp = _revitContext.UIApplication!;
        uiapp.ViewActivated -= ViewActivated;
        uiapp.ViewActivated -= DocumentFocusChanged;
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
