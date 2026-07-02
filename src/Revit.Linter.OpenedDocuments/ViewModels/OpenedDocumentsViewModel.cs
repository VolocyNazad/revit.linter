using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Context.Abstractions.Services;
using Revit.Events.Abstractions.Services;
using Revit.Linter.OpenedDocuments.ViewModels.Base;
using System.Collections.ObjectModel;

namespace Revit.Linter.OpenedDocuments.ViewModels;

[XamlConstructor]
public sealed partial class OpenedDocumentsViewModel : RevitInteractionViewModel
{
    private readonly IRevitContext _revitContext;

    public OpenedDocumentsViewModel(
        IRevitContext revitContext, IAsyncExternalEvent externalEvent) : base(externalEvent)
    {
        _revitContext = revitContext;
    }

    [ObservableProperty]
    public partial ObservableCollection<DocumentViewModel> Collection { get; private set; } = [];

    protected override void OnRevitChanged(RevitEventType revitEventType)
    {
        if (revitEventType is RevitEventType.DocumentClosed 
            or RevitEventType.DocumentCreated or RevitEventType.DocumentOpened)
            Refresh();
    }

    private void Refresh()
    {
        IEnumerable<DocumentViewModel> collection = _revitContext.Application!.Documents
            .Cast<Document>()
            .Select(i => new DocumentViewModel { Title = i.Title, DisplayName = i.Title })
            .Prepend(new DocumentViewModel { Title = string.Empty, DisplayName = "All" });
        Collection = new(collection);
    }
}
