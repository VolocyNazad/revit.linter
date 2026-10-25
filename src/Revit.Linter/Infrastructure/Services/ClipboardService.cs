using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.Infrastructure.Services;

/// <summary>
/// Copies text to the Windows clipboard for the presenter modules.
/// </summary>
internal sealed class ClipboardService(ILogger<ClipboardService> logger) : IClipboardService
{
    /// <inheritdoc />
    public bool TrySetText(string text)
    {
        try
        {
            // The clipboard is shared with every desktop application; a busy owner makes the copy fail
            // with a COM or access error. Copying is a convenience, so the failure is logged and reported
            // to the caller instead of interrupting the command that requested it.
            System.Windows.Clipboard.SetText(text);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to copy {CharacterCount} characters to the clipboard", text.Length);
            return false;
        }
    }
}
