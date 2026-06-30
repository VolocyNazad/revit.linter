using Revit.Linter.DiagnosticReportPresenter.ViewModels;

namespace Revit.Linter.DiagnosticReportPresenter.Views;

public sealed partial class DiagnosticReportView
{
    public DiagnosticReportView(IServiceProvider serviceProvider)
    {
        ServiceLocator.Initialize(serviceProvider);
        InitializeComponent();
    }
}
