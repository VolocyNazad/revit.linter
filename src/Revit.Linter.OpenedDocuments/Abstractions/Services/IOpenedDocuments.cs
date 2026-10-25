using Revit.Linter.OpenedDocuments.Abstractions.Models;
using System.ComponentModel;

namespace Revit.Linter.OpenedDocuments.Abstractions.Services;

/// <summary>
/// Exposes the documents currently open in the Revit session as selector items.
/// </summary>
/// <remarks>
/// This is the contract presenters depend on for the shared document selector, so no presenter references
/// the selector view model. The collection starts with the entry that represents every document and is
/// replaced as a whole when documents open or close; the implementation raises
/// <see cref="INotifyPropertyChanged.PropertyChanged"/> for <see cref="Documents"/> so a bound selector
/// refreshes.
/// </remarks>
public interface IOpenedDocuments : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the selectable documents, starting with the entry that represents all documents.
    /// </summary>
    IReadOnlyList<OpenedDocument> Documents { get; }
}
