namespace Revit.Linter.DiagnosticReportPresenter.Interactions;

public interface IDiagnosticReportPresenter
{
    void Clear();
    void Clear(string documentTitle);
    void Refresh();
}
