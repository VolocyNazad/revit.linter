using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace Revit.Linter.DiagnosticListPresenter.ViewModels;

internal sealed partial class DiagnosticItemViewModel(
    IUserInterfaceActivityStream activityStream,
    IDiagnosticConfigurationWriter configurationWriter,
    ILogger<DiagnosticItemViewModel> logger) : ObservableObject
{
    private DiagnosticState? _state;
    private DiagnosticRuleType? _ruleType;

    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsObsolete { get; private set; }
    public bool IsTour { get; private set; }
    public bool IsDebug { get; private set; }
    public string? ConfigurationPath { get; private set; }
    public bool HasConfiguration => ConfigurationPath is not null;
    public Action? OpenDocumentationDelegate { get; private set; }
    public string ObsoleteDescription { get; private set; } = string.Empty;
    public TargetType TargetType { get; private set; }
    public IReadOnlyList<string> Tags { get; private set; } = DiagnosticTags.None;
    /// <summary>Gets the tags as one text, used to sort the column.</summary>
    public string TagsText => string.Join(", ", Tags);
    public bool HasTags => Tags.Count > 0;

    public bool IsActive
    {
        get => _state?.IsActive ?? false;
        set
        {
            if (_state is null || value == _state.IsActive) return;
            WriteState(isActive: value);
        }
    }

    public DiagnosticSeverity Severity
    {
        get => _state?.Severity ?? default;
        set
        {
            if (_state is null || value == _state.Severity) return;
            WriteState(severity: value);
        }
    }

    public void Initialize(ElementDiagnosticRegistration registration, Action? openDocumentation = null)
    {
        TargetType = TargetType.Element;
        Initialize(registration.Identity, registration.State, registration.RuleType,
            registration.ConfigurationPath, openDocumentation);
    }

    public void Initialize(DocumentDiagnosticRegistration registration, Action? openDocumentation = null)
    {
        TargetType = TargetType.Document;
        Initialize(registration.Identity, registration.State, registration.RuleType,
            registration.ConfigurationPath, openDocumentation);
    }

    [RelayCommand(CanExecute = nameof(HasConfiguration))]
    private void OpenConfiguration()
    {
        if (ConfigurationPath is not { Length: > 0 } path || !File.Exists(path)) return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            activityStream.Publish(new RuleConfigurationOpenedActivity(Code));
        }
        catch (Exception exception) when (exception is Win32Exception or IOException)
        {
            // Best effort: the file has no associated editor or vanished; there is nothing to show.
        }
    }

    /// <summary>Opens the documentation page of the module that registered this diagnostic.</summary>
    [RelayCommand]
    private void OpenDocumentation() => OpenDocumentationDelegate?.Invoke();

    private void Initialize(
        IDiagnosticIdentity identity,
        DiagnosticState state,
        DiagnosticRuleType? ruleType,
        string? configurationPath,
        Action? openDocumentation)
    {
        _state = state;
        _ruleType = ruleType;
        Code = identity.Code;
        Description = identity.Description;
        IsObsolete = identity.IsObsolete;
        IsTour = identity.SystemTags.Contains(DiagnosticTags.Tour);
        IsDebug = identity.SystemTags.Contains(DiagnosticTags.Debug);
        ObsoleteDescription = identity.ObsoleteDescription;
        Tags = identity.Tags;
        ConfigurationPath = configurationPath;
        OpenDocumentationDelegate = openDocumentation;
    }

    private void WriteState(bool? isActive = null, DiagnosticSeverity? severity = null)
    {
        if (_state is null || ConfigurationPath is null) return;

        bool nextIsActive = isActive ?? _state.IsActive;
        DiagnosticSeverity nextSeverity = severity ?? _state.Severity;
        try
        {
            configurationWriter.UpdateState(
                ConfigurationPath,
                Code,
                _ruleType ?? DiagnosticRuleType.BuiltIn,
                nextIsActive,
                nextSeverity,
                _state.IsHidden);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to persist the state of diagnostic {DiagnosticCode} to {ConfigurationPath}",
                Code, ConfigurationPath);
            return;
        }

        if (_state.IsActive != nextIsActive)
        {
            _state.IsActive = nextIsActive;
            OnPropertyChanged(nameof(IsActive));
            activityStream.Publish(new DiagnosticSelectionChangedActivity(Code, nextIsActive));
        }

        if (_state.Severity != nextSeverity)
        {
            _state.Severity = nextSeverity;
            OnPropertyChanged(nameof(Severity));
        }
    }
}
