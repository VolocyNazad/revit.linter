using Revit.Context.Abstractions.Services;
using Serilog.Core;
using Serilog.Events;

namespace Revit.Linter.SerilogEnrichers.Enrichers;

/// <summary>
/// Enriches log events with the add-in id of the current external application.
/// </summary>
internal sealed class RevitAddinIdPropertyEnricher(Func<IRevitContext?> contextFactory, string propertyName) : ILogEventEnricher
{
    private readonly Func<IRevitContext?> _contextFactory = contextFactory;
    private readonly string _propertyName = propertyName;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        try
        {
            var application = _contextFactory()?.Application;
            if (application is null)
                return;

            var addInId = application.ActiveAddInId;
            if (addInId is null)
                return;

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(_propertyName, new
            {
                AddinId = addInId.GetGUID(),
                AddinName = addInId.GetAddInName(),
            }));
        }
        catch
        {
            // Revit API access is best-effort and must never break logging.
        }
    }
}