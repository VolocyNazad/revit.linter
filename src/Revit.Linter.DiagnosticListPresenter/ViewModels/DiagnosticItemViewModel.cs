using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.DiagnosticListPresenter.ViewModels;

[XamlConstructor]
[AutoConstructor]
internal sealed partial class DiagnosticItemViewModel : ObservableObject
{
    private readonly IEnumerable<ElementDiagnosticIdOverrides> _elementDiagnosticIdOverrides;
    private readonly IEnumerable<DocumentDiagnosticIdOverrides> _documentDiagnosticIdOverrides;

    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsObsolete { get; set; }
    public string ObsoleteDescription { get; set; }
    public TargetType TargetType { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }
    partial void OnIsActiveChanged(bool value)
    {
        var elementDiagnosticId = _elementDiagnosticIdOverrides.FirstOrDefault(i => i.Identity.Code == Code);
        if (elementDiagnosticId is not null)
        {
            elementDiagnosticId.IsActive = value;
            return;
        }
        var documentDiagnosticId = _documentDiagnosticIdOverrides.FirstOrDefault(i => i.Identity.Code == Code);
        if (documentDiagnosticId is not null)
        {
            documentDiagnosticId.IsActive = value;
            return;
        }
        throw new InvalidOperationException("Diagnistic id overrides not found");
    }

    [ObservableProperty]
    public partial DiagnosticSeverity Severity { get; set; }
    partial void OnSeverityChanged(DiagnosticSeverity value)
    {
        var elementDiagnosticId = _elementDiagnosticIdOverrides.FirstOrDefault(i => i.Identity.Code == Code);
        if (elementDiagnosticId is not null)
        {
            elementDiagnosticId.Severity = Severity;
            return;
        }
        var documentDiagnosticId = _documentDiagnosticIdOverrides.FirstOrDefault(i => i.Identity.Code == Code);
        if (documentDiagnosticId is not null)
        {
            documentDiagnosticId.Severity = Severity;
            return;
        }
        throw new InvalidOperationException("Diagnistic id overrides not found");
    }
}
