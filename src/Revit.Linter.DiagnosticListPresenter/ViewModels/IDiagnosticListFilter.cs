using System.ComponentModel;

namespace Revit.Linter.DiagnosticListPresenter.ViewModels;

internal interface IDiagnosticListFilter : INotifyPropertyChanged
{
    bool IsActive { get; }
    bool IsValid(DiagnosticItemViewModel item);
}
