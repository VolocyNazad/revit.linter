namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Supplies the warning of an element fix for the finding it is about to change.</summary>
/// <remarks>
/// A fix implements this interface when the warning must name the elements it keeps or deletes, and
/// those names differ from one finding to another. The report shows the warning before it opens the
/// transaction, so a declined confirmation changes nothing. A fix-all run shows
/// <see cref="IElementFixConfirmation.ConfirmationMessage"/> once for every finding instead.
/// </remarks>
public interface IElementFixConfirmationDetails
{
    /// <summary>Gets the warning text for one finding.</summary>
    /// <param name="targetElement">The finding target the fix will change.</param>
    /// <returns>The warning text shown before the transaction.</returns>
    string GetConfirmationMessage(Element targetElement);
}
