using Revit.Context.Abstractions.Services;
using Serilog.Core;
using Serilog.Events;

namespace Revit.Linter.SerilogEnrichers.Enrichers;

/// <summary>
/// Enriches log events with the file path of the currently active document.
/// </summary>
internal sealed class RevitDocumentPathNamePropertyEnricher(Func<IRevitContext?> contextFactory, string propertyName) : ILogEventEnricher
{
    private readonly Func<IRevitContext?> _contextFactory = contextFactory;
    private readonly string _propertyName = propertyName;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        try
        {
            var document = _contextFactory()?.ActiveDocument;
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(_propertyName, document?.PathName));
        }
        catch
        {
            // Revit API access is best-effort and must never break logging.
        }
    }
}