using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.RunDiagnosticPresenter.Views;

namespace Revit.Linter.FixReportPresenter.ViewModels;

internal sealed class ServiceLocator
{
    private static IServiceProvider? ServiceProvider;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public RunDiagnosticView RunDiagnosticView => ServiceProvider?.GetRequiredService<RunDiagnosticView>()
        ?? throw new InvalidOperationException($"{nameof(RunDiagnosticPresenter.Views.RunDiagnosticView)} not found"); //todo using custom exception
}
