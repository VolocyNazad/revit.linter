using Serilog.Core;

namespace Revit.Linter.SerilogEnrichers.Tests;

internal sealed class TestSink : ILogEventSink
{
    public List<LogEvent> LogEvents { get; } = [];

    public void Emit(LogEvent logEvent) => LogEvents.Add(logEvent);
}