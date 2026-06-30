using Revit.Linter.FixReportPresenter.ViewModels;

namespace Revit.Linter.FixReportPresenter.Views;

public sealed partial class FixReportView
{
    public FixReportView(IServiceProvider serviceProvider)
    {
        ServiceLocator.Initialize(serviceProvider);
        InitializeComponent();
    }
}
