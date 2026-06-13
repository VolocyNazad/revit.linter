namespace Revit.Linter.BackgroundDiagnostic.Abstractions.Services;

public interface IBackgroundDiagnosticService
{
    bool Activate(Document document, bool onlyChanged);
    bool Deactivate(Document document);
}


