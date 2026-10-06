using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;

namespace Revit.Linter.DiagnosticListPresenter.ViewModels;

internal sealed partial class DiagnosticItemViewModel(IUserInterfaceActivityStream activityStream) : ObservableObject
{
    private DocumentDiagnosticIdOverride? _documentOverride;
    private ElementDiagnosticIdOverride? _elementOverride;
    private Dispatcher? _dispatcher;

    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsObsolete { get; private set; }
    public bool IsExample { get; private set; }
    public bool IsTour { get; private set; }
    public string? ConfigurationPath { get; private set; }
    public bool HasConfiguration => ConfigurationPath is not null;
    public string ObsoleteDescription { get; private set; } = string.Empty;
    public TargetType TargetType { get; private set; }

    public bool IsActive
    {
        get => _elementOverride?.IsActive ?? _documentOverride?.IsActive ?? false;
        set
        {
            if (value == IsActive) return;
            if (_elementOverride is not null) _elementOverride.IsActive = value;
            else if (_documentOverride is not null) _documentOverride.IsActive = value;
            else return;

            activityStream.Publish(new DiagnosticSelectionChangedActivity(Code, value));
        }
    }

    public DiagnosticSeverity Severity
    {
        get => _elementOverride?.Severity ?? _documentOverride?.Severity ?? default;
        set
        {
            if (_elementOverride is not null) _elementOverride.Severity = value;
            else if (_documentOverride is not null) _documentOverride.Severity = value;
        }
    }

    public void Initialize(ElementDiagnosticIdOverride item, string? configurationPath = null)
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        _elementOverride = item;
        TargetType = TargetType.Element;
        Initialize(item.Identity);
        ConfigurationPath = configurationPath;
        item.Changed += Override_Changed;
    }

    public void Initialize(DocumentDiagnosticIdOverride item, string? configurationPath = null)
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        _documentOverride = item;
        TargetType = TargetType.Document;
        Initialize(item.Identity);
        ConfigurationPath = configurationPath;
        item.Changed += Override_Changed;
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

    private void Initialize(ElementDiagnosticId identity)
    {
        Code = identity.Code;
        Description = identity.Description;
        IsObsolete = identity.IsObsolete;
        IsExample = identity.IsExample;
        IsTour = identity.IsTour;
        ObsoleteDescription = identity.ObsoleteDescription;
    }

    private void Initialize(DocumentDiagnosticId identity)
    {
        Code = identity.Code;
        Description = identity.Description;
        IsObsolete = identity.IsObsolete;
        IsExample = identity.IsExample;
        IsTour = identity.IsTour;
        ObsoleteDescription = identity.ObsoleteDescription;
    }

    private void Override_Changed(object? sender, DiagnosticOverrideChangedEventArgs args)
    {
        Dispatcher? dispatcher = _dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted) return;
        if (!dispatcher.CheckAccess())
        {
            _ = dispatcher.InvokeAsync(() => NotifyChanges(args));
            return;
        }

        NotifyChanges(args);
    }

    private void NotifyChanges(DiagnosticOverrideChangedEventArgs args)
    {
        if (args.Previous.IsActive != args.Current.IsActive)
            OnPropertyChanged(nameof(IsActive));
        if (args.Previous.Severity != args.Current.Severity)
            OnPropertyChanged(nameof(Severity));
    }
}
