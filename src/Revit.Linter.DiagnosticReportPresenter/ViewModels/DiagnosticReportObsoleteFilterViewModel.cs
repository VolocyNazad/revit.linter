using CommunityToolkit.Mvvm.ComponentModel;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

internal sealed partial class DiagnosticReportObsoleteFilterViewModel : ObservableObject, IDiagnosticReportFilter
{
    public DiagnosticReportObsoleteFilterViewModel(bool isActive, int count)
    {
        IsActive = isActive;
        Count = count;
    }

    [ObservableProperty]
    public bool _isActive;

    public int Count { get; init; }

    public bool IsValid(DiagnosticReportItemViewModel item) => item.IsObsolete;

    public override string ToString() => $"{Count} Obsolete";
}
