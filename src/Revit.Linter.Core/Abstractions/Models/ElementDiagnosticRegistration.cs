using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.Core.Abstractions.Models;

public sealed record ElementDiagnosticRegistration(
    ElementDiagnosticId Identity,
    IElementDiagnostic Diagnostic,
    IElementDiagnosticFilter Filter,
    IElementDiagnosticDocumentFilter DocumentFilter,
    ElementDiagnosticIdOverride Override,
    IReadOnlyList<IElementFix> Fixes);
