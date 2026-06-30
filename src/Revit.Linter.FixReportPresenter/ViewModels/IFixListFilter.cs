using System.ComponentModel;

namespace Revit.Linter.FixReportPresenter.ViewModels;

internal interface IFixListFilter : INotifyPropertyChanged
{
    bool IsActive { get; }
    bool IsValid(FixReportItemViewModel item);
}
