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
    private readonly IDiagnosticConfigurationProblemSource _problemSource;
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
        IDiagnosticConfigurationProblemSource problemSource,
        IDiagnosticConfigurationParseErrorSource parseErrorSource)
    {
        _catalog = catalog;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
        _problemSource = problemSource;
        _parseErrorSource = parseErrorSource;
        _catalog.Changed += Catalog_Changed;
        _catalog.RefreshFailed += Catalog_RefreshFailed;

        // Every configuration problem is published through the same state object, so one subscription
        // shows the parse failure, the structured file problems and the skipped-rule summary together.
        _problemSource.Changed += ProblemSource_Changed;

        ShowCurrentProblems();
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
        _problemSource.Changed -= ProblemSource_Changed;
    }

    private void Catalog_Changed(object? sender, DiagnosticCatalogChangedEventArgs args)
    {
        lock (_sync)
        {
            if (_disposed) return;
            _lastError = null;
        }

        if (args.Origin == DiagnosticCatalogChangeOrigin.ExternalFile &&
            _problemSource.CurrentRuleError is null &&
            _problemSource.CurrentProblems.Count == 0 &&
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

    private void ProblemSource_Changed(object? sender, EventArgs args)
    {
        lock (_sync)
            if (_disposed) return;

        ShowCurrentProblems();
    }

    private void ShowCurrentProblems()
    {
        List<string> sections = [];

        if (_parseErrorSource.CurrentError is { } parseError)
            sections.Add(_localizer["userDiagnosticConfiguration_parseFailed_message", parseError.Message]);

        IReadOnlyList<DiagnosticConfigurationFileProblem> problems = _problemSource.CurrentProblems;
        if (problems.Count > 0)
        {
            string details = string.Join(
                Environment.NewLine, problems.Select(FormatProblem));
            sections.Add(_localizer["diagnosticConfiguration_fileProblems_message", details]);
        }

        if (_problemSource.CurrentRuleError is { } ruleError)
            sections.Add(_localizer["diagnosticConfiguration_rulesSkipped_message", ruleError]);

        if (sections.Count > 0)
            ShowMessage(string.Join(Environment.NewLine + Environment.NewLine, sections));
    }

    private string FormatProblem(DiagnosticConfigurationFileProblem problem) => problem.Code switch
    {
        DiagnosticConfigurationFileProblemCode.FileEmpty =>
            _localizer["diagnosticConfiguration_fileEmpty_message", problem.FileName],
        DiagnosticConfigurationFileProblemCode.VersionNewer =>
            _localizer[
                "diagnosticConfiguration_fileVersionNewer_message",
                problem.FileName,
                problem.Arguments[0],
                problem.Arguments[1]],
        DiagnosticConfigurationFileProblemCode.VersionUnsupported =>
            _localizer[
                "diagnosticConfiguration_fileVersionUnsupported_message",
                problem.FileName,
                problem.Arguments[0],
                problem.Arguments[1]],
        DiagnosticConfigurationFileProblemCode.FileUnreadable =>
            _localizer[
                "diagnosticConfiguration_fileUnreadable_message",
                problem.FileName,
                problem.Arguments[0]],
        DiagnosticConfigurationFileProblemCode.EmptyRule =>
            _localizer["diagnosticConfiguration_emptyRule_message", problem.FileName],
        _ => _localizer["diagnosticConfiguration_fileUnreadable_message", problem.FileName, problem.Code.ToString()],
    };

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
