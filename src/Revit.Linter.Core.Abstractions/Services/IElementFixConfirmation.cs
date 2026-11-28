namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Describes the warning a fix shows before it changes the document.</summary>
/// <remarks>
/// A fix asks for confirmation when the finding and the fix name do not tell the user what will
/// change: it touches a file outside the document, or it makes a choice the name does not describe,
/// such as keeping one of several equal elements. The caller shows the message before it opens the
/// transaction, so a declined confirmation changes nothing.
/// </remarks>
public interface IElementFixConfirmation
{
    /// <summary>Gets the warning text shown to the user.</summary>
    string ConfirmationMessage { get; }

    /// <summary>Gets the label of the button that applies the fix.</summary>
    string ConfirmationButtonText { get; }
}
