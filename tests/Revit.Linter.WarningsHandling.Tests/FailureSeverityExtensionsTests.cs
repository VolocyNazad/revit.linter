using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.WarningsHandling.Infrastructure.Extensions;

namespace Revit.Linter.WarningsHandling.Tests;

public sealed class FailureSeverityExtensionsTests : RevitApiTest
{
    [Test]
    public async Task ToDiagnosticSeverity_maps_known_severities()
    {
        (FailureSeverity Source, DiagnosticSeverity Expected)[] cases =
        [
            (FailureSeverity.None, DiagnosticSeverity.Message),
            (FailureSeverity.DocumentCorruption, DiagnosticSeverity.Message),
            (FailureSeverity.Warning, DiagnosticSeverity.Warning),
            (FailureSeverity.Error, DiagnosticSeverity.Error),
        ];

        foreach ((FailureSeverity source, DiagnosticSeverity expected) in cases)
        {
            DiagnosticSeverity result = source.ToDiagnosticSeverity();
            await Assert.That(result).IsEqualTo(expected);
        }
    }
}
