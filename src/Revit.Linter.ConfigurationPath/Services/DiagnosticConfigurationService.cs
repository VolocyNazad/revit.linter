using Revit.Linter.ConfigurationPath.Abstractions.Services;
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
    private readonly object _sync = new();
    private readonly DiagnosticConfigurationErrorState _errorState;
    private readonly IDiagnosticConfigurationRuleTextFormatter? _ruleTextFormatter;
    private readonly ConfigurationFileChangeSource _changeSource;
    private readonly IReadOnlyList<DiagnosticConfigurationFileDescriptor> _files;
    private IReadOnlyList<DiagnosticRuleSource> _rules = [];
    private DateTime _suppressWatcherUntilUtc;
    private bool _dirty = true;
    private bool _disposed;

    /// <summary>Creates the service for the configuration files of the running Revit version.</summary>
    /// <param name="errorState">The state that collects the problems found while reading the files.</param>
    public DiagnosticConfigurationService(DiagnosticConfigurationErrorState errorState)
        : this(CreateDefaultFiles(new ConfigurationPaths()), errorState, null)
    {
    }

    /// <summary>Creates the service for the paths and formatter resolved by the host.</summary>
    /// <param name="errorState">The state that collects the problems found while reading the files.</param>
    /// <param name="paths">The configuration paths of the running Revit version.</param>
    /// <param name="ruleTextFormatter">Formats rule schema problems in the user's language, or <see langword="null"/> for English.</param>
    public DiagnosticConfigurationService(
        DiagnosticConfigurationErrorState errorState,
        IConfigurationPaths paths,
        IDiagnosticConfigurationRuleTextFormatter? ruleTextFormatter)
        : this(CreateDefaultFiles(paths), errorState, ruleTextFormatter)
    {
    }

    /// <summary>Creates the service for explicit configuration files, used by tests.</summary>
    /// <param name="files">The configuration files in source precedence order.</param>
    /// <param name="errorState">The state that collects the problems found while reading the files.</param>
    public DiagnosticConfigurationService(
        IReadOnlyList<DiagnosticConfigurationFileDescriptor> files,
        DiagnosticConfigurationErrorState errorState)
        : this(files, errorState, null)
    {
    }

    private DiagnosticConfigurationService(
        IReadOnlyList<DiagnosticConfigurationFileDescriptor> files,
        DiagnosticConfigurationErrorState errorState,
        IDiagnosticConfigurationRuleTextFormatter? ruleTextFormatter)
    {
        if (files.Count == 0) throw new ArgumentException("At least one configuration file is required.", nameof(files));

        _files = files;
        _errorState = errorState;
        _ruleTextFormatter = ruleTextFormatter;
        _changeSource = new ConfigurationFileChangeSource(files.Select(file => file.Path));
    }

    private static IReadOnlyList<DiagnosticConfigurationFileDescriptor> CreateDefaultFiles(IConfigurationPaths paths) =>
    [
        new(paths.ConfigPath, IsTopLevel: true),
        new(paths.DebugConfigPath, IsDebug: true),
        new(paths.ExampleConfigPath, IsExample: true),
        new(paths.PracticalTourConfigPath, IsTour: true),
    ];

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
        List<string> fileErrors = [];
        List<string> ruleErrors = [];
        List<DiagnosticConfigurationFileProblem> problems = [];
        Exception? parseError = null;

        foreach (DiagnosticConfigurationFileDescriptor descriptor in _files)
        {
            string path = descriptor.Path;
            string fileName = Path.GetFileName(path);
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
                    // The first parse failure is published as the parse error so the notifier shows
                    // exactly one message for it; any further failure joins the skipped-rule summary.
                    if (readError is null)
                    {
                        fileErrors.Add($"{fileName}: the file is not valid YAML");
                    }
                    else if (parseError is null)
                    {
                        parseError = readError;
                    }
                    else
                    {
                        fileErrors.Add($"{fileName}: {readError.Message}");
                        problems.Add(new DiagnosticConfigurationFileProblem(
                            DiagnosticConfigurationFileProblemCode.FileUnreadable, fileName, [readError.Message]));
                    }
                    continue;
                }

                if (file is null)
                {
                    fileErrors.Add($"{fileName}: the file is empty; add version: 2 and a rules list");
                    problems.Add(new DiagnosticConfigurationFileProblem(
                        DiagnosticConfigurationFileProblemCode.FileEmpty, fileName, []));
                    continue;
                }

                if (file.Version > ConfigurationPathUtils.SupportedConfigurationVersion)
                {
                    fileErrors.Add(
                        $"{fileName}: format version {file.Version} is newer than supported version " +
                        $"{ConfigurationPathUtils.SupportedConfigurationVersion}; the file was ignored");
                    problems.Add(new DiagnosticConfigurationFileProblem(
                        DiagnosticConfigurationFileProblemCode.VersionNewer,
                        fileName,
                        [file.Version.ToString(), ConfigurationPathUtils.SupportedConfigurationVersion.ToString()]));
                    continue;
                }

                if (file.Version != ConfigurationPathUtils.SupportedConfigurationVersion)
                {
                    fileErrors.Add(
                        $"{fileName}: unsupported format version {file.Version}; " +
                        $"expected version {ConfigurationPathUtils.SupportedConfigurationVersion}");
                    problems.Add(new DiagnosticConfigurationFileProblem(
                        DiagnosticConfigurationFileProblemCode.VersionUnsupported,
                        fileName,
                        [file.Version.ToString(), ConfigurationPathUtils.SupportedConfigurationVersion.ToString()]));
                    continue;
                }

                foreach (DiagnosticRule rule in file.Rules ?? [])
                {
                    if (rule is null)
                    {
                        fileErrors.Add($"{fileName}: an empty rule entry was ignored");
                        problems.Add(new DiagnosticConfigurationFileProblem(
                            DiagnosticConfigurationFileProblemCode.EmptyRule, fileName, []));
                        continue;
                    }

                    IReadOnlyList<DiagnosticRuleIssue> ruleIssues = DiagnosticRuleValidator.ValidateIssues(rule);
                    if (ruleIssues.Count == 0)
                    {
                        rules.Add(new DiagnosticRuleSource(
                            rule, path, descriptor.IsExample, descriptor.IsDebug, descriptor.IsTour));
                        continue;
                    }

                    string code = string.IsNullOrWhiteSpace(rule.Code) ? "(no code)" : rule.Code ?? "(no code)";
                    foreach (DiagnosticRuleIssue issue in ruleIssues)
                    {
                        string text = _ruleTextFormatter is null
                            ? DiagnosticRuleValidator.Describe(issue)
                            : _ruleTextFormatter.Format(issue.Code.ToString(), issue.Arguments);
                        ruleErrors.Add($"{fileName}: rule '{code}' was skipped: {text}");
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // An unreadable file is a skipped source, not a YAML parse failure.
                fileErrors.Add($"{fileName}: {exception.Message}");
                problems.Add(new DiagnosticConfigurationFileProblem(
                    DiagnosticConfigurationFileProblemCode.FileUnreadable, fileName, [exception.Message]));
            }
        }

        _rules = rules.AsReadOnly();
        _dirty = false;
        _errorState.SetFileErrors(fileErrors, problems, parseError);
        _errorState.AddRuleErrors(ruleErrors);
    }
}

