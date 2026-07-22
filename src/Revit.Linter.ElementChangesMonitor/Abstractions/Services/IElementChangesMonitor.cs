namespace Revit.Linter.ElementChangesMonitor.Abstractions.Services;

public interface IElementChangesMonitor
{
    public bool Run();
    public bool Stop();
}

