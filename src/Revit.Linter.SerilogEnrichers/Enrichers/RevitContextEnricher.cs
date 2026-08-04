using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Revit.Context.Abstractions.Services;
using Revit.Linter.SerilogEnrichers.Utils;
using Serilog.Core;
using Serilog.Events;

namespace Revit.Linter.SerilogEnrichers.Enrichers;

/// <summary>
/// Enriches log events with all Revit context properties in a single pass.
/// The context is resolved once per event and cached after it becomes available,
/// so the dependency container is queried only a single time.
/// </summary>
internal sealed class RevitContextEnricher(Func<IRevitContext?> contextFactory) : ILogEventEnricher
{
    private readonly Func<IRevitContext?> _contextFactory = contextFactory;

    private IRevitContext? _context;
    private LogEventProperty? _version;
    private LogEventProperty? _build;
    private LogEventProperty? _userName;
    private LogEventProperty? _language;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = GetContext();
        if (context is null)
            return;

        EnrichApplication(logEvent, propertyFactory, context);
        EnrichDocument(logEvent, propertyFactory, context);
    }

    private IRevitContext? GetContext()
    {
        var context = _context;
        if (context is not null)
            return context;

        try
        {
            context = _contextFactory();
            if (context is not null)
                _context = context;
        }
        catch
        {
            // Context resolution is best-effort and must never break logging.
        }

        return context;
    }

    private void EnrichApplication(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, IRevitContext context)
    {
        try
        {
            if (context.ControlledApplication is { } controlledApplication)
            {
                if (int.TryParse(controlledApplication.VersionNumber, out int version))
                    AddCached(logEvent, propertyFactory, ref _version, RevitPropertyNames.Version, version);
                AddCached(logEvent, propertyFactory, ref _build, RevitPropertyNames.Build, controlledApplication.VersionBuild);
                AddCached(logEvent, propertyFactory, ref _language, RevitPropertyNames.Language, controlledApplication.Language.GetCultureInfo().EnglishName);
            }

            if (context.Application is { } application)
            {
                AddCached(logEvent, propertyFactory, ref _userName, RevitPropertyNames.UserName, application.Username);
                Add(logEvent, propertyFactory, RevitPropertyNames.AddinId, CreateAddinId(application));
            }
        }
        catch
        {
            // Revit API access is best-effort and must never break logging.
        }
    }

    private static void EnrichDocument(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, IRevitContext context)
    {
        try
        {
            var document = context.ActiveDocument;
            Add(logEvent, propertyFactory, RevitPropertyNames.DocumentTitle, document?.Title);
            Add(logEvent, propertyFactory, RevitPropertyNames.DocumentPathName, document?.PathName);
            Add(logEvent, propertyFactory, RevitPropertyNames.DocumentModelPath, GetModelPath(document));
        }
        catch
        {
            // Revit API access is best-effort and must never break logging.
        }
    }

    private static object? CreateAddinId(Application application)
    {
        var addInId = application.ActiveAddInId;
        if (addInId is null)
            return null;

        return new { AddinId = addInId.GetGUID(), AddinName = addInId.GetAddInName() };
    }

    private static string? GetModelPath(Document? document)
    {
        if (document is { IsModelInCloud: true })
            return ModelPathUtils.ConvertModelPathToUserVisiblePath(document.GetCloudModelPath());

        if (document is { IsWorkshared: true })
            return ModelPathUtils.ConvertModelPathToUserVisiblePath(document.GetWorksharingCentralModelPath());

        return null;
    }

    private static void Add(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, string propertyName, object? value)
        => logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(propertyName, value));

    private static void AddCached(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, ref LogEventProperty? cache, string propertyName, object? value)
    {
        if (cache is null && value is not null)
            cache = propertyFactory.CreateProperty(propertyName, value);

        if (cache is not null)
            logEvent.AddPropertyIfAbsent(cache);
    }
}