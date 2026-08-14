using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.ValueStore.Abstractions;
using System.IO;
using System.Windows.Threading;

namespace Revit.Linter.Infrastructure.Services;

internal sealed class ValueStoreNotifier : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IStringLocalizer<GlobalLocalizations> _localizer;
    private readonly ILogger<ValueStoreNotifier> _logger;
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    private readonly IDisposable _subscription;

    public ValueStoreNotifier(
        IValueStoreNotificationSource source,
        IServiceProvider serviceProvider,
        IStringLocalizer<GlobalLocalizations> localizer,
        ILogger<ValueStoreNotifier> logger)
    {
        _serviceProvider = serviceProvider;
        _localizer = localizer;
        _logger = logger;
        _subscription = source.OnLoadFailed(LoadFailed);
    }

    public void Dispose() => _subscription.Dispose();

    private void LoadFailed(ValueStoreLoadFailedEventArgs args)
    {
        if (_dispatcher.HasShutdownStarted) return;
        _ = _dispatcher.InvokeAsync(() =>
        {
            try
            {
                string message = _localizer[
                    "settingsFile_loadFailed_message",
                    Path.GetFileName(args.FilePath),
                    args.Exception.Message];
                _ = _serviceProvider.GetRequiredService<IDialog>().Show(new DialogRequest(message));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to show a settings file error");
            }
        });
    }
}