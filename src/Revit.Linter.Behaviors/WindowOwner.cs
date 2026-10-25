using System.Windows;
using System.Windows.Interop;

namespace Revit.Linter.Behaviors;

/// <summary>
/// Makes a linter window a child of the window the user is currently working in.
/// </summary>
/// <remarks>
/// A dialog or wizard shown from a Revit add-in has no owner of its own. Without one it can appear behind
/// Revit or lose its Z-order against a second linter window. The active linter window wins; otherwise the
/// Revit main window is used, and when Revit exposes no window the window is centered on the screen so it
/// is at least visible.
/// </remarks>
public static class WindowOwner
{
    /// <summary>
    /// Assigns the owner of the specified window and keeps it above that owner.
    /// </summary>
    /// <param name="window">The window to own.</param>
    /// <param name="fallbackOwnerHandle">The main window handle used when no linter window is active.</param>
    public static void Apply(Window window, IntPtr fallbackOwnerHandle)
    {
        if (window is null)
            throw new ArgumentNullException(nameof(window));

        Window? activeWindow = Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(candidate => candidate.IsActive && !ReferenceEquals(candidate, window));
        if (activeWindow is not null)
        {
            window.Owner = activeWindow;
            return;
        }

        if (fallbackOwnerHandle != IntPtr.Zero)
        {
            _ = new WindowInteropHelper(window) { Owner = fallbackOwnerHandle };
            return;
        }

        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }
}
