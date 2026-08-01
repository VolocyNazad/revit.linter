using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.Core.Abstractions.Models;

public sealed record DocumentDiagnosticRegistration(
    DocumentDiagnosticId Identity,
    IDocumentDiagnostic Diagnostic,
    IDocumentDiagnosticFilter Filter,
    DocumentDiagnosticIdOverride Override,
    IReadOnlyList<IDocumentFix> Fixes);
