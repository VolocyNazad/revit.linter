using Revit.Context.Abstractions.Services;
using Revit.Linter.BackgroundDiagnostic.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Services;
using System.Text;

namespace Revit.Linter.BackgroundDiagnostic.Services;

public class BackgroundDiagnosticService(
    IRevitContext revitContext, IDiagnosticService diagnosticService) : IBackgroundDiagnosticService
{
    private static readonly ElementFilter _elementFilter = new LogicalOrFilter(
        new ElementIsElementTypeFilter(true), new ElementIsElementTypeFilter(false));
    private static readonly ChangeType _changeType = Element.GetChangeTypeAny();

    private readonly HashSet<RegisteredUpdaterInfo> _registeredUpdaterInfo = [];

    public bool Activate(Document document, bool onlyChanged)
    {
        var id = Guid.NewGuid();

        IUpdater updater = new UpdaterBuilder()
           .SetUpdaterId(new(revitContext.ControlledApplication!.ActiveAddInId, id))
           .SetChangePriority(ChangePriority.Structure)
           .SetUpdaterName("Revit linter")
           .SetAdditionalInformation("Revit linter background service")
           .SetAction(data =>
           {
               if (!onlyChanged)
               {
                   var ids = new FilteredElementCollector(data.GetDocument()).WherePasses(_elementFilter).ToElementIds();
                   diagnosticService.Excecute(data.GetDocument(), ids);
                   return;
               }
               HashSet<ElementId> elementIds = [];
               elementIds.UnionWith(data.GetAddedElementIds());
               elementIds.UnionWith(data.GetModifiedElementIds());
               diagnosticService.Excecute(data.GetDocument(), elementIds);
           }).Build();

        UpdaterId updaterId = updater.GetUpdaterId();

        if (UpdaterRegistry.IsUpdaterRegistered(updaterId, document)) return false;

        UpdaterRegistry.RegisterUpdater(updater, document, true);
        UpdaterRegistry.AddTrigger(updaterId, document, _elementFilter, _changeType);

        var documentTitle = GetTitleWithoutExtension(document);

        _registeredUpdaterInfo.Add(new(documentTitle, updaterId));

        return true;
    }

    public bool Deactivate(Document document)
    {
        var documentTitle = GetTitleWithoutExtension(document);

        RegisteredUpdaterInfo? info = _registeredUpdaterInfo
            .FirstOrDefault(i => i.DocumentTitle == documentTitle);
        if (info is null) return false;

        UpdaterId updaterId = info.UpdaterId;

        if (!UpdaterRegistry.IsUpdaterRegistered(updaterId, document)) return false;

        UpdaterRegistry.UnregisterUpdater(updaterId, document);

        _registeredUpdaterInfo.Remove(info);

        return true;
    }

    public static string GetTitleWithoutExtension(Document doc)
    {
        string text = doc.Title;
        if (text.EndsWith(".rvt") || text.EndsWith(".rte") || text.EndsWith(".rfa") || text.EndsWith(".rft"))
        {
            string[] array = text.Split('.');
            int num = array.Length;
            text = array.Take(num - 1).Aggregate(new StringBuilder(), (StringBuilder current, string next) => current.Append((current.Length == 0) ? string.Empty : ".").Append(next)).ToString();
        }

        return text;
    }

    private record RegisteredUpdaterInfo(string DocumentTitle, UpdaterId UpdaterId);
}

