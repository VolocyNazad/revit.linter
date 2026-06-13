using System.ComponentModel;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

internal interface IDiagnosticReportFilter : INotifyPropertyChanged
{
    bool IsActive { get; }
    bool IsValid(DiagnosticReportItemViewModel item);
}
