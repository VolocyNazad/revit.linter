using CommunityToolkit.Mvvm.ComponentModel;

namespace Revit.Linter.DiagnosticListPresenter.ViewModels;

internal sealed partial class DiagnosticTargetTypeFilterViewModel : ObservableObject, IDiagnosticListFilter
{
    public DiagnosticTargetTypeFilterViewModel(TargetType value, int count)
    {
        Value = value;
        Count = count;
        IsActive = true;
    }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    public TargetType Value { get; }
    public int Count { get; }

    public bool IsValid(DiagnosticItemViewModel item) => item.TargetType == Value;

    public override string ToString() => $"{Count} {Value}";
}
