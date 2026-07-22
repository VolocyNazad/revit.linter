using Autodesk.Revit.DB;
using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.WarningsHandling.Infrastructure.Extensions;

internal static class FailureSeverityExtensions
{
    extension(FailureSeverity failureSeverity)
    {
        public DiagnosticSeverity ToDiagnosticSeverity()
            => failureSeverity switch {
                FailureSeverity.None => DiagnosticSeverity.Message,
                FailureSeverity.DocumentCorruption => DiagnosticSeverity.Message,
                FailureSeverity.Warning => DiagnosticSeverity.Warning,
                FailureSeverity.Error => DiagnosticSeverity.Error,
                _ => throw new NotImplementedException(
                    $"{nameof(FailureSeverity)} contains not mapped with {nameof(DiagnosticSeverity)} variant"),
            };
    }
}
