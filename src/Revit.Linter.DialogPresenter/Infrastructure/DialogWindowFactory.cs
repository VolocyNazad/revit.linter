using Revit.Context.Abstractions.Services;
using Revit.Linter.DialogPresenter.Views;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using System.Windows;
using System.Windows.Interop;

namespace Revit.Linter.DialogPresenter.Infrastructure;

/// <summary>
/// Creates the dialog windows bound to their view models.
/// </summary>
/// <remarks>
/// Window creation stays here, on the module composition boundary, so view models never reference
/// concrete views. Showing, cancellation and result cleanup stay with the caller.
/// </remarks>
internal sealed class DialogWindowFactory(IThemeService themeService, IRevitContext revitContext)
{
    /// <summary>Creates the informational dialog window for the given view model.</summary>
    public Window CreateDialog(object dataContext) => Create(new DialogView(), dataContext);

    /// <summary>Creates the confirmation dialog window for the given view model.</summary>
    public Window CreateConfirmationDialog(object dataContext) => Create(new ConfirmationDialogView(), dataContext);

    private Window Create(Window window, object dataContext)
    {
        window.DataContext = dataContext;
        themeService.Register(window);

        _ = new WindowInteropHelper(window) {
            Owner = revitContext.UIApplication!.MainWindowHandle
        };

        return window;
    }
}
