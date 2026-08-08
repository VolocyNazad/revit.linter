using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using System.Threading;
using System.Windows.Threading;

namespace Revit.Linter.Infrastructure.Services;

internal sealed class FormulaCompilationNotifier(
    IServiceProvider serviceProvider,
    IStringLocalizer<GlobalLocalizations> localizer,
    ILogger<FormulaCompilationNotifier> logger) : IFormulaCompilationNotifier
{
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    private int _notificationRequested;

    public void Notify()
    {
        if (Interlocked.Exchange(ref _notificationRequested, 1) != 0 ||
            _dispatcher.HasShutdownStarted)
            return;

        _ = _dispatcher.InvokeAsync(() =>
        {
            try
            {
                _ = serviceProvider.GetRequiredService<IDialog>().Show(
                    new DialogRequest(localizer["formulaCompilation_failed_message"]));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to show a formula compilation error");
            }
        });
    }
}
