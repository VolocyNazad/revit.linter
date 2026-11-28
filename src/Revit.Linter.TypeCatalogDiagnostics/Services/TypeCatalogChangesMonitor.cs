using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
using Microsoft.Extensions.Logging;
using Revit.Context.Abstractions.Services;
using Revit.Linter.ElementChangesProvider.Abstractions.Models;
using Revit.Linter.ElementChangesProvider.Abstractions.Services;
using Revit.Linter.TypeCatalogDiagnostics.Abstractions.Services;
using Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalog;

namespace Revit.Linter.TypeCatalogDiagnostics.Services;

/// <summary>
/// Watches the type catalog file of every open saved family and asks the report to check the family
/// again when that file changes.
/// </summary>
/// <remarks>
/// Revit raises no event for a file changed outside the model, so the linter would keep the findings
/// of the previous catalog until a full run. The monitor turns such a change into an element change
/// of the family element, which is the target of every family type catalog check, and the report
/// replaces its rows the same way it does after a model edit.
/// </remarks>
internal sealed class TypeCatalogChangesMonitor : ITypeCatalogChangesMonitor
{
    private readonly IRevitContext _revitContext;
    private readonly IRevitEventSource _revitEventSource;
    private readonly IRevitIdlingScheduler _idlingScheduler;
    private readonly IElementChangesSender _elementChangesSender;
    private readonly ILogger<TypeCatalogChangesMonitor> _logger;
    private readonly TypeCatalogFileWatcher _watcher = new();
    private readonly object _pendingSync = new();
    // A catalog write raises several file events: the paths are collected and checked together.
    private readonly HashSet<string> _pendingPaths = new(StringComparer.OrdinalIgnoreCase);
    private bool _idlingScheduled;
    private volatile bool _running;

    public TypeCatalogChangesMonitor(
        IRevitContext revitContext,
        IRevitEventSource revitEventSource,
        IRevitIdlingScheduler idlingScheduler,
        IElementChangesSender elementChangesSender,
        ILogger<TypeCatalogChangesMonitor> logger)
    {
        _revitContext = revitContext;
        _revitEventSource = revitEventSource;
        _idlingScheduler = idlingScheduler;
        _elementChangesSender = elementChangesSender;
        _logger = logger;
        _watcher.Changed += Watcher_Changed;
    }

    public bool Run()
    {
        if (_running) return false;

        _running = true;
        _revitEventSource.Changed += RevitEventSource_Changed;
        RefreshWatchSet();
        return true;
    }

    public bool Stop()
    {
        if (!_running) return false;

        _running = false;
        _revitEventSource.Changed -= RevitEventSource_Changed;
        _watcher.ReplaceWatchSet([]);

        lock (_pendingSync)
        {
            _pendingPaths.Clear();
        }

        return true;
    }

    // Which files to watch follows the open documents, so every document lifecycle event rebuilds the
    // set. A model edit cannot change a document path and fires far too often to be worth the rebuild.
    private void RevitEventSource_Changed(RevitEventType revitEventType)
    {
        if (revitEventType == RevitEventType.DocumentChanged) return;
        RefreshWatchSet();
    }

    // Called on the Revit thread, where the open documents and their paths are readable.
    private void RefreshWatchSet()
    {
        if (!_running) return;

        try
        {
            Application? application = _revitContext.Application;
            if (application is null) return;

            List<string> catalogPaths = [];
            foreach (Document document in application.Documents.Cast<Document>())
            {
                if (!document.IsValidObject || !document.IsFamilyDocument) continue;

                string path = document.PathName;
                if (!FamilyTypeCatalogReader.IsSavedFamilyFile(path)) continue;

                catalogPaths.Add(FamilyTypeCatalogReader.CatalogPathFor(path));
            }

            _watcher.ReplaceWatchSet(catalogPaths);
        }
        catch (Exception exception)
        {
            // A watcher failure must not break the Revit event that raised it or the add-in start.
            _logger.LogError(exception, "Failed to update the watched family type catalogs");
        }
    }

    // Called on a file-system thread, so it only records the path and moves the check to Idling.
    private void Watcher_Changed(string catalogPath)
    {
        if (!_running) return;

        lock (_pendingSync)
        {
            _pendingPaths.Add(catalogPath);
            if (_idlingScheduled) return;
            _idlingScheduled = true;
        }

        try
        {
            _ = _idlingScheduler.RunAsync(_ => RecheckPendingChanges());
        }
        catch (ObjectDisposedException)
        {
            // The add-in is shutting down and the scheduled work will never run; its rows go with the
            // session, so there is nothing to update.
        }
        catch (Exception exception)
        {
            lock (_pendingSync)
            {
                _idlingScheduled = false;
            }

            _logger.LogError(exception, "Failed to schedule a family check after a type catalog change");
        }
    }

    // Called on the Revit thread: the documents are readable and the report is asked for a new check.
    private void RecheckPendingChanges()
    {
        string[] catalogPaths;
        lock (_pendingSync)
        {
            catalogPaths = [.. _pendingPaths];
            _pendingPaths.Clear();
            _idlingScheduled = false;
        }

        if (!_running) return;

        try
        {
            Application? application = _revitContext.Application;
            if (application is null) return;

            foreach (string catalogPath in catalogPaths)
            {
                foreach (Document document in application.Documents.Cast<Document>())
                {
                    if (!document.IsValidObject || !document.IsFamilyDocument) continue;
                    if (!FamilyTypeCatalogReader.IsCatalogOf(document.PathName, catalogPath)) continue;
                    if (document.OwnerFamily is not { } family) continue;

                    _logger.LogInformation(
                        "The type catalog {CatalogName} of {DocumentTitle} changed; the family findings are checked again",
                        Path.GetFileName(catalogPath), document.Title);
                    _elementChangesSender.Send(new ElementChanges(
                        document, [], [family.Id], [], UndoOperation.TransactionCommitted));
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to recheck a family after its type catalog changed");
        }
    }
}
