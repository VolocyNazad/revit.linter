using CommunityToolkit.Mvvm.ComponentModel;

namespace Revit.Linter.FixReportPresenter.ViewModels;

internal sealed partial class FixReportItemViewModel : ObservableObject
{
    public required string Message { get; set; }
}
