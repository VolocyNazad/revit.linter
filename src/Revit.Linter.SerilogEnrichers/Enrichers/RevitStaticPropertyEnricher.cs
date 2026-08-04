using Revit.Context.Abstractions.Services;
using Serilog.Core;
using Serilog.Events;

namespace Revit.Linter.SerilogEnrichers.Enrichers;

/// <summary>
/// Reads a static Revit property on the first log event where the value is available,
/// caches it as a <see cref="LogEventProperty"/>, then reuses it for every subsequent event.
/// </summary>
internal sealed class RevitStaticPropertyEnricher(
    Func<IRevitContext?> contextFactory,
    Func<IRevitContext, object?> valueSelector,
    string propertyName) : ILogEventEnricher
{
    private readonly Func<IRevitContext?> _contextFactory = contextFactory;
    private readonly Func<IRevitContext, object?> _valueSelector = valueSelector;
    private readonly string _propertyName = propertyName;

    private LogEventProperty? _property;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (_property is not null)
        {
            logEvent.AddPropertyIfAbsent(_property);
            return;
        }

        try
        {
            var context = _contextFactory();
            if (context is null)
                return;

            var value = _valueSelector(context);
            if (value is null)
                return;

            _property = propertyFactory.CreateProperty(_propertyName, value);
            logEvent.AddPropertyIfAbsent(_property);
        }
        catch
        {
            // Revit API access is best-effort and must never break logging.
        }
    }
}