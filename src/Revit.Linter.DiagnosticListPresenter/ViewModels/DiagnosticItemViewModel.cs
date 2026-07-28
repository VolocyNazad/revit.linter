using CommunityToolkit.Mvvm.ComponentModel;

namespace Revit.Linter.DiagnosticListPresenter.ViewModels;

[XamlConstructor]
internal sealed partial class DiagnosticItemViewModel : ObservableObject
{
    private DocumentDiagnosticIdOverride? _documentOverride;
    private ElementDiagnosticIdOverride? _elementOverride;

    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsObsolete { get; private set; }
    public string ObsoleteDescription { get; private set; } = string.Empty;
    public TargetType TargetType { get; private set; }

    public bool IsActive
    {
        get => _elementOverride?.IsActive ?? _documentOverride?.IsActive ?? false;
        set
        {
            if (_elementOverride is not null) _elementOverride.IsActive = value;
            else if (_documentOverride is not null) _documentOverride.IsActive = value;
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

    public void Initialize(ElementDiagnosticIdOverride item)
    {
        _elementOverride = item;
        TargetType = TargetType.Element;
        Initialize(item.Identity);
        item.Changed += Override_Changed;
    }

    public void Initialize(DocumentDiagnosticIdOverride item)
    {
        _documentOverride = item;
        TargetType = TargetType.Document;
        Initialize(item.Identity);
        item.Changed += Override_Changed;
    }

    private void Initialize(ElementDiagnosticId identity)
    {
        Code = identity.Code;
        Description = identity.Description;
        IsObsolete = identity.IsObsolete;
        ObsoleteDescription = identity.ObsoleteDescription;
    }

    private void Initialize(DocumentDiagnosticId identity)
    {
        Code = identity.Code;
        Description = identity.Description;
        IsObsolete = identity.IsObsolete;
        ObsoleteDescription = identity.ObsoleteDescription;
    }

    private void Override_Changed(object? sender, DiagnosticOverrideChangedEventArgs args)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
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
