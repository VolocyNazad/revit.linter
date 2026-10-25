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
    private readonly IDiagnosticConfigurationErrorSource _configurationErrorSource;
    private readonly IDiagnosticConfigurationParseErrorSource _parseErrorSource;
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    private readonly object _sync = new();
    private string? _lastError;
    private bool _disposed;

    public DiagnosticCatalogNotifier(
        IDiagnosticCatalog catalog,
        IServiceProvider serviceProvider,
        ILogger<DiagnosticCatalogNotifier> logger,
        IStringLocalizer<GlobalLocalizations> localizer,
        IDiagnosticConfigurationErrorSource configurationErrorSource,
        IDiagnosticConfigurationParseErrorSource parseErrorSource)
    {
        _catalog = catalog;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
        _configurationErrorSource = configurationErrorSource;
        _parseErrorSource = parseErrorSource;
        _catalog.Changed += Catalog_Changed;
        _catalog.RefreshFailed += Catalog_RefreshFailed;
        _configurationErrorSource.Changed += ConfigurationErrorSource_Changed;
        _parseErrorSource.Changed += ParseErrorSource_Changed;

        ShowCurrentConfigurationError();
        ShowCurrentParseError();
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
        _configurationErrorSource.Changed -= ConfigurationErrorSource_Changed;
        _parseErrorSource.Changed -= ParseErrorSource_Changed;
    }

    private void Catalog_Changed(object? sender, DiagnosticCatalogChangedEventArgs args)
    {
        lock (_sync)
        {
            if (_disposed) return;
            _lastError = null;
        }

        if (args.Origin == DiagnosticCatalogChangeOrigin.ExternalFile &&
            _configurationErrorSource.CurrentError is null &&
            _parseErrorSource.CurrentError is null)
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

    private void ConfigurationErrorSource_Changed(object? sender, EventArgs args)
    {
        lock (_sync)
            if (_disposed) return;

        ShowCurrentConfigurationError();
    }

    private void ParseErrorSource_Changed(object? sender, EventArgs args)
    {
        lock (_sync)
            if (_disposed) return;

        ShowCurrentParseError();
    }

    private void ShowCurrentConfigurationError()
    {
        string? error = _configurationErrorSource.CurrentError;
        if (error is not null)
            ShowMessage(_localizer["diagnosticConfiguration_rulesSkipped_message", error]);
    }

    private void ShowCurrentParseError()
    {
        Exception? error = _parseErrorSource.CurrentError;
        if (error is not null)
            ShowMessage(_localizer["userDiagnosticConfiguration_parseFailed_message", error.Message]);
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
