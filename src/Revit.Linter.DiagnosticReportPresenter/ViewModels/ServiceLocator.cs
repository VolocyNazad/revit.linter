using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.OpenedDocuments.ViewModels;
using Revit.Linter.RunDiagnosticPresenter.Views;
using System.Diagnostics.CodeAnalysis;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

internal sealed class ServiceLocator
{
    private static IServiceProvider? ServiceProvider;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    [SuppressMessage("SonarAnalyzer", "S2325", Justification = "The property is used in XAML bindings and must be an instance property.")]
    public RunDiagnosticView RunDiagnosticView => ServiceProvider?.GetRequiredService<RunDiagnosticView>()
        ?? throw new InvalidOperationException($"Service provider not initialized.");

    [SuppressMessage("SonarAnalyzer", "S2325", Justification = "The property is used in XAML bindings and must be an instance property.")]
    public OpenedDocumentsViewModel OpenedDocumentsViewModel => ServiceProvider?.GetRequiredService<OpenedDocumentsViewModel>()
       ?? throw new InvalidOperationException($"Service provider not initialized.");
}
