using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.DocumentDiagnostics.DI;
using Revit.Linter.DocumentDiagnostics.Diagnostics.MissingSizeTable;
using Revit.Linter.DocumentDiagnostics.Diagnostics.MissingSizeTableColumn;
using Revit.Linter.DocumentDiagnostics.Diagnostics.SizeTables;
using Revit.Linter.DocumentDiagnostics.Diagnostics.UnusedSizeTables;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.Testing;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class DocumentDiagnosticsRegistrationTests
{
    [Test]
    public async Task Size_table_components_are_resolvable()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDocumentQueryService>(TestDocumentQueries.Create());
        services.AddDocumentDiagnostics();
        using ServiceProvider provider = services.BuildServiceProvider();

        SizeTableReferenceCollector? collector = provider.GetService<SizeTableReferenceCollector>();
        UnusedSizeTablesDiagnostic unused =
            ActivatorUtilities.CreateInstance<UnusedSizeTablesDiagnostic>(provider);
        DeleteUnusedSizeTablesFix fix =
            ActivatorUtilities.CreateInstance<DeleteUnusedSizeTablesFix>(provider);
        MissingSizeTableDiagnostic missingTable =
            ActivatorUtilities.CreateInstance<MissingSizeTableDiagnostic>(provider);
        MissingSizeTableColumnDiagnostic missingColumn =
            ActivatorUtilities.CreateInstance<MissingSizeTableColumnDiagnostic>(provider);

        await Assert.That(collector).IsNotNull();
        await Assert.That(unused).IsNotNull();
        await Assert.That(fix).IsNotNull();
        await Assert.That(missingTable).IsNotNull();
        await Assert.That(missingColumn).IsNotNull();
    }
}
