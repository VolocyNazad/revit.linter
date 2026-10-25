using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Localization;
using Revit.Linter.Presentation.ViewModels;
using System.Collections.ObjectModel;

namespace Revit.Linter.OpenedDocuments.ViewModels;

/// <summary>
/// Provides the selectable collection of non-linked documents currently open in Revit.
/// </summary>
[GenerateLocalizedProperties]
[XamlConstructor]
public sealed partial class OpenedDocumentsViewModel : RevitInteractionViewModel
{
    private readonly IRevitContext _revitContext;

    /// <summary>
    /// Initializes the view model with the current Revit context and the observed Revit events.
    /// </summary>
    /// <param name="revitContext">Provides access to the current Revit application.</param>
    /// <param name="revitEventSource">Publishes the Revit application events the view model reacts to.</param>
    public OpenedDocumentsViewModel(
        IRevitContext revitContext, IRevitEventSource revitEventSource) : base(revitEventSource)
    {
        _revitContext = revitContext;
    }

    /// <summary>
    /// Gets the document selector items, starting with the localized item that represents all documents.
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<DocumentViewModel> Collection { get; private set; } = [];

    /// <inheritdoc />
    protected override void OnRevitChanged(RevitEventType revitEventType)
    {
        if (revitEventType is RevitEventType.DocumentClosed 
            or RevitEventType.DocumentCreated or RevitEventType.DocumentOpened)
            Refresh();
    }

    private void Refresh()
    {
        IEnumerable<DocumentViewModel> collection = _revitContext.Application!.Documents
            .Cast<Document>().Where(i => !i.IsLinked)
            .Select(i => new DocumentViewModel { Title = i.Title, DisplayName = i.Title })
            .Prepend(new DocumentViewModel { Title = string.Empty, DisplayName = AllDocumentsText });
        Collection = new(collection);
    }
}
