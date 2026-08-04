using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DialogPresenter.Abstractions;
using System.Windows.Threading;

namespace Revit.Linter.Infrastructure.Services;

internal sealed class DiagnosticCatalogNotifier : IDisposable
{
    private readonly IDiagnosticCatalog _catalog;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DiagnosticCatalogNotifier> _logger;
    private readonly IStringLocalizer<GlobalLocalizations> _localizer;
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    private readonly object _sync = new();
    private string? _lastError;
    private bool _disposed;

    public DiagnosticCatalogNotifier(
        IDiagnosticCatalog catalog,
        IServiceProvider serviceProvider,
        ILogger<DiagnosticCatalogNotifier> logger,
        IStringLocalizer<GlobalLocalizations> localizer)
    {
        _catalog = catalog;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
        _catalog.Changed += Catalog_Changed;
        _catalog.RefreshFailed += Catalog_RefreshFailed;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
        }

        _catalog.Changed -= Catalog_Changed;
        _catalog.RefreshFailed -= Catalog_RefreshFailed;
    }

    private void Catalog_Changed(object? sender, DiagnosticCatalogChangedEventArgs args)
    {
        lock (_sync)
        {
            if (_disposed) return;
            _lastError = null;
        }

        if (args.Origin == DiagnosticCatalogChangeOrigin.ExternalFile)
            ShowMessage(_localizer["diagnosticCatalog_externalChange_message"]);
    }

    private void Catalog_RefreshFailed(object? sender, DiagnosticCatalogRefreshFailedEventArgs args)
    {
        string errorKey = $"{args.Exception.GetType().FullName}:{args.Exception.Message}";
        lock (_sync)
        {
            if (_disposed || string.Equals(_lastError, errorKey, StringComparison.Ordinal)) return;
            _lastError = errorKey;
        }

        ShowMessage(_localizer["diagnosticCatalog_refreshFailed_message", args.Exception.Message]);
    }

    private void ShowMessage(string content)
    {
        if (_dispatcher.HasShutdownStarted) return;
        _ = _dispatcher.InvokeAsync(() =>
        {
            try
            {
                _ = _serviceProvider.GetRequiredService<IDialog>().Show(new DialogRequest(content));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to show a diagnostic catalog notification");
            }
        });
    }
}
