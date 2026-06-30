using Revit.Linter.DiagnosticListPresenter.ViewModels;

namespace Revit.Linter.DiagnosticListPresenter.Views;

public sealed partial class DiagnosticListView
{
    public DiagnosticListView(IServiceProvider serviceProvider)
    {
        ServiceLocator.Initialize(serviceProvider);
        InitializeComponent();
    }
}
