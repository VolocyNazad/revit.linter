using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DiagnosticConfiguration.Models;
using Revit.Linter.DiagnosticConfiguration.Validation;

namespace Revit.Linter.ConfigurationPath.Services;

/// <summary>
/// Reads and watches the diagnostic configuration files shared by every diagnostic module.
/// </summary>
/// <remarks>
/// The service reads each file once per catalog refresh, validates every entry against the schema of its
/// declared type, and returns the valid entries in source precedence order. Missing top-level files are
/// created with the format header; missing debug, example and tour files are simply skipped, so the
/// product does not create folders for configurations the user never installed.
/// </remarks>
public sealed class DiagnosticConfigurationService : IDiagnosticConfigurationService, IDiagnosticCatalogChangeSource, IDisposable
{
    private static readonly DiagnosticConfigurationFileDescriptor[] _defaultFiles =
    [
        new(ConfigurationPathUtils.ConfigPath, IsTopLevel: true),
        new(ConfigurationPathUtils.DebugConfigPath, IsDebug: true),
        new(ConfigurationPathUtils.ExampleConfigPath, IsExample: true),
        new(ConfigurationPathUtils.PracticalTourConfigPath, IsTour: true),
    ];

    private readonly object _sync = new();
    private readonly DiagnosticConfigurationErrorState _errorState;
    private readonly ConfigurationFileChangeSource _changeSource;
    private readonly IReadOnlyList<DiagnosticConfigurationFileDescriptor> _files;
    private IReadOnlyList<DiagnosticRuleSource> _rules = [];
    private DateTime _suppressWatcherUntilUtc;
    private bool _dirty = true;
    private bool _disposed;

    /// <summary>Creates the service for the configuration files of the running Revit version.</summary>
    /// <param name="errorState">The state that collects the problems found while reading the files.</param>
    public DiagnosticConfigurationService(DiagnosticConfigurationErrorState errorState)
        : this(_defaultFiles, errorState)
    {
    }

    /// <summary>Creates the service for explicit configuration files, used by tests.</summary>
    /// <param name="files">The configuration files in source precedence order.</param>
    /// <param name="errorState">The state that collects the problems found while reading the files.</param>
    public DiagnosticConfigurationService(
        IReadOnlyList<DiagnosticConfigurationFileDescriptor> files,
        DiagnosticConfigurationErrorState errorState)
    {
        if (files.Count == 0) throw new ArgumentException("At least one configuration file is required.", nameof(files));

        _files = files;
        _errorState = errorState;
        _changeSource = new ConfigurationFileChangeSource(files.Select(file => file.Path));
    }

    /// <inheritdoc />
    public string ConfigurationPath => _files.FirstOrDefault(file => file.IsTopLevel)?.Path ?? _files[0].Path;

    /// <summary>Gets the configuration files in source precedence order.</summary>
    public IReadOnlyList<DiagnosticConfigurationFileDescriptor> Files => _files;

    /// <summary>Marks the cached rules as stale after the product itself wrote a configuration file.</summary>
    public void Invalidate()
    {
        lock (_sync)
            _dirty = true;
    }

    /// <inheritdoc />
    public IReadOnlyList<DiagnosticRuleSource> GetRules()
    {
        lock (_sync)
        {
            if (_dirty) Rebuild();
            return _rules;
        }
    }

    /// <inheritdoc />
    public IDisposable OnChange(Action listener)
    {
        if (listener is null) throw new ArgumentNullException(nameof(listener));
        return _changeSource.OnChange(() =>
        {
            lock (_sync)
            {
                if (_disposed || DateTime.UtcNow < _suppressWatcherUntilUtc) return;
                _dirty = true;
            }

            listener();
        });
    }

    /// <summary>
    /// Ignores the file-system events produced by a write the product itself is performing.
    /// </summary>
    /// <param name="duration">How long the events are ignored.</param>
    /// <remarks>
    /// The product writes only the state fields of a rule, so an ignored event does not hide a change in the
    /// set of registered diagnostics. The caller refreshes the catalog explicitly when the set changes.
    /// </remarks>
    public void SuppressWatcherChanges(TimeSpan duration)
    {
        lock (_sync)
            _suppressWatcherUntilUtc = DateTime.UtcNow + duration;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
        }

        _changeSource.Dispose();
    }

    private void Rebuild()
    {
        List<DiagnosticRuleSource> rules = [];
        List<string> errors = [];
        Exception? parseError = null;

        foreach (DiagnosticConfigurationFileDescriptor descriptor in _files)
        {
            string path = descriptor.Path;
            try
            {
                if (!File.Exists(path))
                {
                    if (!descriptor.IsTopLevel) continue;
                    ConfigurationPathUtils.EnsureConfigurationFile(path);
                }

                if (!ConfigurationPathUtils.TryGetDiagnosticConfiguration(
                        path, out DiagnosticConfigurationFile? file, out Exception? readError))
                {
                    errors.Add($"{Path.GetFileName(path)}: {readError?.Message ?? "the file is not valid YAML"}");
                    parseError ??= readError;
                    continue;
                }

                if (file is null)
                {
                    errors.Add($"{Path.GetFileName(path)}: the file is empty; add version: 2 and a rules list");
                    continue;
                }

                if (file.Version > ConfigurationPathUtils.SupportedConfigurationVersion)
                {
                    errors.Add(
                        $"{Path.GetFileName(path)}: format version {file.Version} is newer than supported version " +
                        $"{ConfigurationPathUtils.SupportedConfigurationVersion}; the file was ignored");
                    continue;
                }

                if (file.Version != ConfigurationPathUtils.SupportedConfigurationVersion)
                {
                    errors.Add(
                        $"{Path.GetFileName(path)}: unsupported format version {file.Version}; " +
                        $"expected version {ConfigurationPathUtils.SupportedConfigurationVersion}");
                    continue;
                }

                foreach (DiagnosticRule rule in file.Rules ?? [])
                {
                    if (rule is null)
                    {
                        errors.Add($"{Path.GetFileName(path)}: an empty rule entry was ignored");
                        continue;
                    }

                    IReadOnlyList<string> ruleErrors = DiagnosticRuleValidator.Validate(rule);
                    if (ruleErrors.Count == 0)
                    {
                        rules.Add(new DiagnosticRuleSource(
                            rule, path, descriptor.IsExample, descriptor.IsDebug, descriptor.IsTour));
                        continue;
                    }

                    string code = string.IsNullOrWhiteSpace(rule.Code) ? "(no code)" : rule.Code;
                    foreach (string ruleError in ruleErrors)
                        errors.Add($"{Path.GetFileName(path)}: rule '{code}' was skipped: {ruleError}");
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                errors.Add($"{Path.GetFileName(path)}: {exception.Message}");
                parseError ??= exception;
            }
        }

        _rules = rules.AsReadOnly();
        _dirty = false;
        _errorState.SetFileErrors(errors, parseError);
    }
}
