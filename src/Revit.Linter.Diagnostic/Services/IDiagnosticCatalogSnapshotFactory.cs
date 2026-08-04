namespace Revit.Linter.Diagnostic.Services;

internal interface IDiagnosticCatalogSnapshotFactory
{
    DiagnosticCatalogSnapshotOwner Create();
}
