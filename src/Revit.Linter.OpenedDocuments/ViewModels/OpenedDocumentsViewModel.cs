using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Localization;
using Revit.Linter.OpenedDocuments.Abstractions.Models;
using Revit.Linter.OpenedDocuments.Abstractions.Services;
using Revit.Linter.Presentation.ViewModels;

namespace Revit.Linter.OpenedDocuments.ViewModels;

/// <summary>
/// Provides the selectable collection of non-linked documents currently open in Revit.
/// </summary>
/// <remarks>
/// Presenters consume the selector through <see cref="IOpenedDocuments"/>; this view model is an implementation
/// detail of the module and the composition root registers it under that contract.
/// </remarks>
[GenerateLocalizedProperties]
[XamlConstructor]
public sealed partial class OpenedDocumentsViewModel : RevitInteractionViewModel, IOpenedDocuments
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

    /// <inheritdoc />
    [ObservableProperty]
    public partial IReadOnlyList<OpenedDocument> Documents { get; private set; } = [];

    /// <inheritdoc />
    protected override void OnRevitChanged(RevitEventType revitEventType)
    {
        if (revitEventType is RevitEventType.DocumentClosed
            or RevitEventType.DocumentCreated or RevitEventType.DocumentOpened)
            Refresh();
    }

    private void Refresh()
    {
        IEnumerable<OpenedDocument> documents = _revitContext.Application!.Documents
            .Cast<Document>().Where(document => !document.IsLinked)
            .Select(document => new OpenedDocument { Title = document.Title, DisplayName = document.Title })
            .Prepend(new OpenedDocument { Title = string.Empty, DisplayName = AllDocumentsText });
        Documents = documents.ToArray();
    }
}
