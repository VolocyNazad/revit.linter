using Revit.Context.Abstractions.Services;
using Revit.Linter.Behaviors;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using Revit.Linter.WelcomePresenter.Views;
using System.Windows;

namespace Revit.Linter.WelcomePresenter.Infrastructure.Services;

/// <summary>
/// Creates the welcome wizard window bound to its view model.
/// </summary>
/// <remarks>
/// Window creation stays here, on the module composition boundary, so the view model never
/// references the concrete view. Showing and result handling stay with the caller. The shared
/// <see cref="WindowOwner"/> mechanic keeps the wizard above the window the user works in.
/// </remarks>
internal sealed class WelcomeWindowFactory(IThemeService themeService, IRevitContext revitContext)
{
    /// <summary>Creates the welcome wizard window for the given view model.</summary>
    public Window Create(object dataContext)
    {
        WelcomeView window = new() { DataContext = dataContext };
        themeService.Register(window);
        WindowOwner.Apply(window, revitContext.UIApplication?.MainWindowHandle ?? IntPtr.Zero);

        return window;
    }
}
