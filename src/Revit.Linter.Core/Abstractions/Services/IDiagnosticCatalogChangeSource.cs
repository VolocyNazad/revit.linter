namespace Revit.Linter.Core.Abstractions.Services;

public interface IDiagnosticCatalogChangeSource
{
    IDisposable OnChange(Action listener);
}
