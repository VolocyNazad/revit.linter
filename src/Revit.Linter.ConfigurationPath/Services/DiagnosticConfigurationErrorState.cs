using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ConfigurationPath.Services;

/// <summary>Collects the configuration problems of one catalog refresh and exposes them as one message.</summary>
public sealed class DiagnosticConfigurationErrorState
    : IDiagnosticConfigurationErrorState, IDiagnosticConfigurationParseErrorSource, IDiagnosticConfigurationProblemSource
{
    private readonly object _sync = new();
    private readonly List<string> _pendingFileMessages = [];
    private readonly List<string> _pendingRuleMessages = [];
    private readonly List<DiagnosticConfigurationFileProblem> _pendingProblems = [];
    private IReadOnlyList<DiagnosticConfigurationFileProblem> _currentProblems = [];
    private string? _currentError;
    private string? _currentRuleError;
    private Exception? _currentParseError;
    private Exception? _pendingParseError;
    private bool _refreshInProgress;

    /// <inheritdoc />
    public string? CurrentError
    {
        get
        {
            lock (_sync) return _currentError;
        }
    }

    /// <inheritdoc />
    string? IDiagnosticConfigurationProblemSource.CurrentRuleError
    {
        get
        {
            lock (_sync) return _currentRuleError;
        }
    }

    /// <inheritdoc />
    IReadOnlyList<DiagnosticConfigurationFileProblem> IDiagnosticConfigurationProblemSource.CurrentProblems
    {
        get
        {
            lock (_sync) return _currentProblems;
        }
    }

    /// <inheritdoc />
    Exception? IDiagnosticConfigurationParseErrorSource.CurrentError
    {
        get
        {
            lock (_sync) return _currentParseError;
        }
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public void BeginRefresh()
    {
        lock (_sync)
        {
            _refreshInProgress = true;
            _pendingFileMessages.Clear();
            _pendingRuleMessages.Clear();
            _pendingProblems.Clear();
            _pendingParseError = null;
        }
    }

    /// <inheritdoc />
    public void SetFileErrors(IReadOnlyList<string> messages, Exception? parseError)
        => SetFileErrors(messages, [], parseError);

    /// <summary>Replaces the file-level problems, their plain descriptions and the parsing failure.</summary>
    /// <param name="messages">The plain English descriptions, kept for logs and legacy callers.</param>
    /// <param name="problems">The structured problems the host can localize.</param>
    /// <param name="parseError">The parsing failure to expose separately, or <see langword="null"/>.</param>
    public void SetFileErrors(
        IReadOnlyList<string> messages,
        IReadOnlyList<DiagnosticConfigurationFileProblem> problems,
        Exception? parseError)
    {
        bool changed;
        lock (_sync)
        {
            _pendingFileMessages.Clear();
            _pendingFileMessages.AddRange(messages);
            _pendingProblems.Clear();
            _pendingProblems.AddRange(problems);
            _pendingParseError = parseError;
            changed = !_refreshInProgress && Publish();
        }

        if (changed) Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void AddRuleErrors(IEnumerable<string> messages)
    {
        bool changed;
        lock (_sync)
        {
            foreach (string message in messages)
            {
                if (string.IsNullOrWhiteSpace(message)) continue;
                if (!_pendingRuleMessages.Contains(message, StringComparer.Ordinal))
                    _pendingRuleMessages.Add(message);
            }

            changed = !_refreshInProgress && Publish();
        }

        if (changed) Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void CompleteRefresh()
    {
        bool changed;
        lock (_sync)
        {
            _refreshInProgress = false;
            changed = Publish();
        }

        if (changed) Changed?.Invoke(this, EventArgs.Empty);
    }

    private bool Publish()
    {
        string? ruleError = Join(_pendingRuleMessages);
        string? description = Join(_pendingFileMessages.Concat(_pendingRuleMessages).ToArray());
        DiagnosticConfigurationFileProblem[] problems = _pendingProblems.ToArray();
        bool changed = !string.Equals(_currentError, description, StringComparison.Ordinal)
                       || !string.Equals(_currentRuleError, ruleError, StringComparison.Ordinal)
                       || !ReferenceEquals(_currentParseError, _pendingParseError)
                       || !ProblemsEqual(_currentProblems, problems);
        _currentError = description;
        _currentRuleError = ruleError;
        _currentParseError = _pendingParseError;
        _currentProblems = problems;
        return changed;
    }

    private static string? Join(IReadOnlyList<string> messages)
        => messages.Count == 0 ? null : string.Join(Environment.NewLine, messages);

    private static bool ProblemsEqual(
        IReadOnlyList<DiagnosticConfigurationFileProblem> previous,
        IReadOnlyList<DiagnosticConfigurationFileProblem> current)
    {
        if (previous.Count != current.Count) return false;

        for (int index = 0; index < current.Count; index++)
        {
            DiagnosticConfigurationFileProblem left = previous[index];
            DiagnosticConfigurationFileProblem right = current[index];
            if (left.Code != right.Code
                || !string.Equals(left.FileName, right.FileName, StringComparison.Ordinal)
                || !left.Arguments.SequenceEqual(right.Arguments))
                return false;
        }

        return true;
    }
}
