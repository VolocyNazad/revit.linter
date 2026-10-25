using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.ConfigurationPath.Services;

/// <summary>Collects the configuration problems of one catalog refresh and exposes them as one message.</summary>
public sealed class DiagnosticConfigurationErrorState
    : IDiagnosticConfigurationErrorState, IDiagnosticConfigurationParseErrorSource
{
    private readonly object _sync = new();
    private readonly List<string> _pendingMessages = [];
    private string? _currentError;
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
            _pendingMessages.Clear();
            _pendingParseError = null;
        }
    }

    /// <inheritdoc />
    public void SetFileErrors(IReadOnlyList<string> messages, Exception? parseError)
    {
        bool changed;
        lock (_sync)
        {
            _pendingMessages.Clear();
            _pendingMessages.AddRange(messages);
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
                if (!_pendingMessages.Contains(message, StringComparer.Ordinal))
                    _pendingMessages.Add(message);
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
        string? description = _pendingMessages.Count == 0
            ? null
            : string.Join(Environment.NewLine, _pendingMessages);
        bool changed = !string.Equals(_currentError, description, StringComparison.Ordinal)
                       || !ReferenceEquals(_currentParseError, _pendingParseError);
        _currentError = description;
        _currentParseError = _pendingParseError;
        return changed;
    }
}
