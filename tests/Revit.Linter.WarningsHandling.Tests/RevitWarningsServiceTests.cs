using Autodesk.Revit.DB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using Revit.Linter.WarningsHandling.Abstractions.Models;
using Revit.Linter.WarningsHandling.Abstractions.Services;
using Revit.Linter.WarningsHandling.DI;
using TUnit.Core.Executors;

namespace Revit.Linter.WarningsHandling.Tests;

public sealed class RevitWarningsServiceTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Dependency_injection_registers_service_as_singleton()
    {
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender);

        IRevitWarningsService first = services.GetRequiredService<IRevitWarningsService>();
        IRevitWarningsService second = services.GetRequiredService<IRevitWarningsService>();

        await Assert.That(ReferenceEquals(first, second)).IsTrue();
    }

    [Test]
    public async Task Execute_succeeds_without_warnings_and_sends_no_reports()
    {
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender);

        WarningsServiceResult result = services.GetRequiredService<IRevitWarningsService>()
            .Execute(_document!);

        await Assert.That(result).IsEqualTo(WarningsServiceResult.Success);
        await Assert.That(sender.Reports).IsEmpty();
    }

    [Test]
    public async Task Execute_returns_failed_for_invalid_document()
    {
        ReportSender sender = new();
        using ServiceProvider services = CreateServices(sender);

        WarningsServiceResult result = services.GetRequiredService<IRevitWarningsService>()
            .Execute(null!);

        await Assert.That(result).IsEqualTo(WarningsServiceResult.Failed);
        await Assert.That(sender.Reports).IsEmpty();
    }

    private static ServiceProvider CreateServices(IDiagnosticReportSender sender)
    {
        ServiceCollection services = new();
        services.AddSingleton(sender);
        services.AddSingleton<IDiagnosticReportSender>(sender);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddRevitWarningsModule();
        return services.BuildServiceProvider();
    }

    private sealed class ReportSender : IDiagnosticReportSender
    {
        public List<DiagnosticReport> Reports { get; } = [];
        public void Send(DiagnosticReport report) => Reports.Add(report);
    }
}
