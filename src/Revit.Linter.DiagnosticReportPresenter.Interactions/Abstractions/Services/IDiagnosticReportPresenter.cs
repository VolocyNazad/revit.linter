namespace Revit.Linter.DiagnosticReportPresenter.Interactions.Abstractions.Services;

public interface IDiagnosticReportPresenter
{
    void Clear();
    void Clear(string documentTitle);
    void Refresh();
}
