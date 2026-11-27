namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Describes the warning a document fix shows before it changes the document.</summary>
/// <remarks>
/// A fix that deletes data a user may not expect to lose asks for confirmation. The caller shows the
/// message before it opens the transaction, so a declined confirmation changes nothing.
/// </remarks>
public interface IDocumentFixConfirmation
{
    /// <summary>Gets the warning text shown to the user.</summary>
    string ConfirmationMessage { get; }

    /// <summary>Gets the label of the button that applies the fix.</summary>
    string ConfirmationButtonText { get; }
}
