using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.SizeTableDiagnostics.DI;
using Revit.Linter.SizeTableDiagnostics.Diagnostics.MissingSizeTable;
using Revit.Linter.SizeTableDiagnostics.Diagnostics.MissingSizeTableColumn;
using Revit.Linter.SizeTableDiagnostics.Diagnostics.UnresolvedSizeTableArgument;
using Revit.Linter.SizeTableDiagnostics.Diagnostics.UnusedSizeTables;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class SizeTableDiagnosticsRegistrationTests
{
    [Test]
    public async Task Size_table_components_are_resolvable()
    {
        var services = new ServiceCollection();
        services.AddSizeTableDiagnostics();
        using ServiceProvider provider = services.BuildServiceProvider();

        UnusedSizeTablesDiagnostic unused =
            ActivatorUtilities.CreateInstance<UnusedSizeTablesDiagnostic>(provider);
        DeleteUnusedSizeTablesFix fix =
            ActivatorUtilities.CreateInstance<DeleteUnusedSizeTablesFix>(provider);
        MissingSizeTableDiagnostic missingTable =
            ActivatorUtilities.CreateInstance<MissingSizeTableDiagnostic>(provider);
        MissingSizeTableColumnDiagnostic missingColumn =
            ActivatorUtilities.CreateInstance<MissingSizeTableColumnDiagnostic>(provider);
        UnresolvedSizeTableArgumentDiagnostic unresolvedArgument =
            ActivatorUtilities.CreateInstance<UnresolvedSizeTableArgumentDiagnostic>(provider);

        await Assert.That(unused).IsNotNull();
        await Assert.That(fix).IsNotNull();
        await Assert.That(missingTable).IsNotNull();
        await Assert.That(missingColumn).IsNotNull();
        await Assert.That(unresolvedArgument).IsNotNull();
    }

    [Test]
    public async Task Provider_registers_every_size_table_diagnostic()
    {
        var services = new ServiceCollection();
        services.AddSizeTableDiagnostics();
        services.AddSingleton<IBuiltInDiagnosticStateProvider>(new TestBuiltInStateProvider());
        using ServiceProvider provider = services.BuildServiceProvider();

        string[] codes = provider.GetServices<IDiagnosticRegistrationProvider>()
            .SelectMany(registrationProvider => registrationProvider.GetDocumentDiagnostics())
            .Select(registration => registration.Identity.Code)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        await Assert.That(codes).IsEquivalentTo(["DOC002", "DOC005", "DOC006", "DOC007"]);
    }

    private sealed class TestBuiltInStateProvider : IBuiltInDiagnosticStateProvider
    {
        public string ConfigurationPath => string.Empty;

        public IReadOnlyList<string> OverriddenCodes => [];

        public DiagnosticState GetState(
            string code, DiagnosticSeverity defaultSeverity, bool defaultIsActive) =>
            new(defaultIsActive, defaultSeverity, false);
    }
}
