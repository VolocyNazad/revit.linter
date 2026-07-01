using CommunityToolkit.Mvvm.ComponentModel;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

internal sealed partial class DiagnosticReportActualFilterViewModel : ObservableObject, IDiagnosticReportFilter
{
    public DiagnosticReportActualFilterViewModel(bool isActive, int count)
    {
        IsActive = isActive;
        Count = count;
    }

    [ObservableProperty]
    public bool _isActive;

    public int Count { get; init; }

    public bool IsValid(DiagnosticReportItemViewModel item) => !item.IsObsolete;

    public override string ToString() => $"{Count} Actual";
}
