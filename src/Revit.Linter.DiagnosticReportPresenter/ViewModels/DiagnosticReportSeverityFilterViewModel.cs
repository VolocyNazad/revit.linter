using CommunityToolkit.Mvvm.ComponentModel;
using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

internal sealed partial class DiagnosticReportSeverityFilterViewModel : ObservableObject, IDiagnosticReportFilter
{
    public DiagnosticReportSeverityFilterViewModel(bool isActive, DiagnosticSeverity value, int count)
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

    public override string ToString() => $"{Count} {Value}";
}