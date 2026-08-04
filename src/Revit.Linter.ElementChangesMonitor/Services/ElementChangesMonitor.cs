using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Revit.Context.Abstractions.Services;
using Revit.Linter.ElementChangesMonitor.Abstractions.Services;
using Revit.Linter.ElementChangesProvider.Abstractions.Services;

namespace Revit.Linter.ElementChangesMonitor.Services;

internal sealed class ElementChangesMonitor(
    IRevitContext revitContext,
    IElementChangesSender elementChangesSender) : IElementChangesMonitor
{
    private ControlledApplication? _application;

    public bool Run()
    {
        if (_application is not null) return false;

        _application = revitContext.ControlledApplication!;
        _application.DocumentChanged += Application_DocumentChanged;
        return true;
    }

    public bool Stop()
    {
        if (_application is null) return false;

        _application.DocumentChanged -= Application_DocumentChanged;
        _application = null;
        return true;
    }

    private void Application_DocumentChanged(object? sender, DocumentChangedEventArgs args)
    {
        ICollection<ElementId> created = args.GetAddedElementIds();
        ICollection<ElementId> modified = args.GetModifiedElementIds();
        ICollection<ElementId> deleted = args.GetDeletedElementIds();
        if (created.Count == 0 && modified.Count == 0 && deleted.Count == 0) return;

        elementChangesSender.Send(new(args.GetDocument(), created, modified, deleted));
    }
}
