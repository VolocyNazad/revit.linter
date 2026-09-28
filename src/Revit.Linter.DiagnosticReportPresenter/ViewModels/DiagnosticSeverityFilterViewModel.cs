using CommunityToolkit.Mvvm.ComponentModel;

using Revit.Linter.Localization;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

internal sealed partial class DiagnosticSeverityFilterViewModel : ObservableObject, IDiagnosticReportFilter
{
    public DiagnosticSeverityFilterViewModel(bool isActive, DiagnosticSeverity value, int count)
    {
        IsActive = isActive;
        Value = value;
        Count = count;
    }

    [ObservableProperty]
    public bool _isActive;

    public DiagnosticSeverity Value { get; init; }
    public int Count { get; init; }

    public bool IsValid(DiagnosticReportItemViewModel item) => item.Severity == Value;

    public override string ToString()
        => $"{Count} {DiagnosticSeverityLocalizations.GetString(Value.ToString())}";
}
