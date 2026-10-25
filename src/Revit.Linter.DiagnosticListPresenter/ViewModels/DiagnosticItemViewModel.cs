using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Localization;

namespace Revit.Linter.DiagnosticListPresenter.ViewModels;

internal sealed partial class DiagnosticItemViewModel(
    IUserInterfaceActivityStream activityStream,
    IDiagnosticConfigurationWriter configurationWriter,
    IShellLauncher shellLauncher,
    ILogger<DiagnosticItemViewModel> logger) : ObservableObject
{
    private const string ResourceBaseName =
        "Revit.Linter.Localization.DiagnosticListPresenter.ViewModels.DiagnosticListViewModel";

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

    /// <summary>Gets the callback that tells the owning list which row changed its enabled state.</summary>
    /// <remarks>
    /// The row does not publish the selection activity itself: only the owning list knows every enabled
    /// diagnostic, so the list publishes one activity with the complete enabled set after each change.
    /// </remarks>
    public Action<DiagnosticItemViewModel>? SelectionChangedDelegate { get; private set; }

    public string ObsoleteDescription { get; private set; } = string.Empty;
    public TargetType TargetType { get; private set; }
    public IReadOnlyList<string> Tags { get; private set; } = DiagnosticTags.None;
    /// <summary>Gets the tags as one text, used to sort the column.</summary>
    public string TagsText => string.Join(", ", Tags);
    public bool HasTags => Tags.Count > 0;

    // The XAML row menu binds to instance members, so these labels stay instance properties.
#pragma warning disable S2325 // Members can be made static
    /// <summary>Gets the menu label that opens the file with its associated application.</summary>
    public string OpenConfigurationText => LocalizationResourceReader.GetString(ResourceBaseName, "openConfiguration_text");

    /// <summary>Gets the menu label that shows the Windows "Open with" dialog.</summary>
    public string OpenConfigurationWithText => LocalizationResourceReader.GetString(ResourceBaseName, "openConfigurationWith_text");

    /// <summary>Gets the menu label that opens the file in Notepad.</summary>
    public string OpenConfigurationInNotepadText => LocalizationResourceReader.GetString(ResourceBaseName, "openConfigurationInNotepad_text");

    /// <summary>Gets the menu label that shows the file selected in File Explorer.</summary>
    public string RevealConfigurationInExplorerText => LocalizationResourceReader.GetString(ResourceBaseName, "revealConfigurationInExplorer_text");
#pragma warning restore S2325 // Members can be made static

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

    public void Initialize(
        ElementDiagnosticRegistration registration,
        Action? openDocumentation = null,
        Action<DiagnosticItemViewModel>? selectionChanged = null)
    {
        TargetType = TargetType.Element;
        Initialize(registration.Identity, registration.State, registration.RuleType,
            registration.ConfigurationPath, openDocumentation, selectionChanged);
    }

    public void Initialize(
        DocumentDiagnosticRegistration registration,
        Action? openDocumentation = null,
        Action<DiagnosticItemViewModel>? selectionChanged = null)
    {
        TargetType = TargetType.Document;
        Initialize(registration.Identity, registration.State, registration.RuleType,
            registration.ConfigurationPath, openDocumentation, selectionChanged);
    }

    [RelayCommand(CanExecute = nameof(HasConfiguration))]
    private void OpenConfiguration() =>
        LaunchConfiguration(static (launcher, path) => launcher.Open(path), publishOpenedActivity: true);

    [RelayCommand(CanExecute = nameof(HasConfiguration))]
    private void OpenConfigurationWith() =>
        LaunchConfiguration(static (launcher, path) => launcher.OpenWith(path), publishOpenedActivity: true);

    [RelayCommand(CanExecute = nameof(HasConfiguration))]
    private void OpenConfigurationInNotepad() =>
        LaunchConfiguration(static (launcher, path) => launcher.OpenInNotepad(path), publishOpenedActivity: true);

    [RelayCommand(CanExecute = nameof(HasConfiguration))]
    private void RevealConfigurationInExplorer() =>
        LaunchConfiguration(static (launcher, path) => launcher.RevealInExplorer(path), publishOpenedActivity: false);

    /// <summary>Opens the documentation page of the module that registered this diagnostic.</summary>
    [RelayCommand]
    private void OpenDocumentation() => OpenDocumentationDelegate?.Invoke();

    private void LaunchConfiguration(
        Func<IShellLauncher, string, bool> launch,
        bool publishOpenedActivity)
    {
        if (ConfigurationPath is not { Length: > 0 } path) return;
        if (!launch(shellLauncher, path)) return;
        // Showing the file in Explorer is not opening it, so the tour does not see it as opening the rule.
        if (publishOpenedActivity)
            activityStream.Publish(new RuleConfigurationOpenedActivity(Code));
    }

    private void Initialize(
        IDiagnosticIdentity identity,
        DiagnosticState state,
        DiagnosticRuleType? ruleType,
        string? configurationPath,
        Action? openDocumentation,
        Action<DiagnosticItemViewModel>? selectionChanged)
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
        SelectionChangedDelegate = selectionChanged;
    }

    /// <summary>Creates the state change a bulk command persists for this row.</summary>
    /// <param name="isActive">The execution state the row must receive.</param>
    /// <returns>The change to persist, or <see langword="null"/> when the row has no configuration to write to.</returns>
    public DiagnosticRuleStateUpdate? CreateStateUpdate(bool isActive)
        => _state is null || ConfigurationPath is null
            ? null
            : new DiagnosticRuleStateUpdate(
                Code, _ruleType ?? DiagnosticRuleType.BuiltIn, isActive, _state.Severity, _state.IsHidden);

    /// <summary>Applies a state a bulk write has already persisted, without writing the configuration again.</summary>
    /// <param name="isActive">The persisted execution state.</param>
    /// <param name="severity">The persisted severity.</param>
    public void ApplyPersistedState(bool isActive, DiagnosticSeverity severity)
    {
        if (_state is null) return;

        if (_state.IsActive != isActive)
        {
            _state.IsActive = isActive;
            OnPropertyChanged(nameof(IsActive));
            SelectionChangedDelegate?.Invoke(this);
        }

        if (_state.Severity != severity)
        {
            _state.Severity = severity;
            OnPropertyChanged(nameof(Severity));
        }
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

        ApplyPersistedState(nextIsActive, nextSeverity);
    }
}
