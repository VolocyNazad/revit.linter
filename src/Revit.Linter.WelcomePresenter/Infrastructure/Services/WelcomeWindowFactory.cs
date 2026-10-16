using Revit.Context.Abstractions.Services;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using Revit.Linter.WelcomePresenter.Views;
using System;
using System.Windows;
using System.Windows.Interop;

namespace Revit.Linter.WelcomePresenter.Infrastructure.Services;

/// <summary>
/// Creates the welcome wizard window bound to its view model.
/// </summary>
/// <remarks>
/// Window creation stays here, on the module composition boundary, so the view model never
/// references the concrete view. Showing and result handling stay with the caller.
/// </remarks>
internal sealed class WelcomeWindowFactory(IThemeService themeService, IRevitContext revitContext)
{
    /// <summary>Creates the welcome wizard window for the given view model.</summary>
    public Window Create(object dataContext)
    {
        WelcomeView window = new() { DataContext = dataContext };
        themeService.Register(window);

        IntPtr owner = revitContext.UIApplication?.MainWindowHandle ?? IntPtr.Zero;
        if (owner == IntPtr.Zero)
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        else
            _ = new WindowInteropHelper(window) { Owner = owner };

        return window;
    }
}
