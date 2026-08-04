namespace Revit.Linter.SerilogEnrichers.Tests;

public sealed class RevitContextEnrichersTests
{
    private static readonly string[] RevitPropertyNames =
    [
        RevitContextLoggerConfigurationExtensions.RevitVersionPropertyName,
        RevitContextLoggerConfigurationExtensions.RevitBuildPropertyName,
        RevitContextLoggerConfigurationExtensions.RevitUserNamePropertyName,
        RevitContextLoggerConfigurationExtensions.RevitLanguagePropertyName,
        RevitContextLoggerConfigurationExtensions.RevitAddinIdPropertyName,
        RevitContextLoggerConfigurationExtensions.RevitDocumentTitlePropertyName,
        RevitContextLoggerConfigurationExtensions.RevitDocumentPathNamePropertyName,
        RevitContextLoggerConfigurationExtensions.RevitDocumentModelPathPropertyName,
    ];

    [Fact]
    public void No_revit_properties_added_without_revit_context()
    {
        var testSink = new TestSink();
        using var logger = new LoggerConfiguration()
            .Enrich.WithRevitContext(() => null)
            .WriteTo.Sink(testSink)
            .CreateLogger();

        logger.Information("Hello, world!");

        var logEvent = Assert.Single(testSink.LogEvents);
        Assert.DoesNotContain(RevitPropertyNames, name => logEvent.Properties.ContainsKey(name));
    }

    [Fact]
    public void Logging_does_not_throw_before_context_is_initialized()
    {
        var testSink = new TestSink();
        using var logger = new LoggerConfiguration()
            .Enrich.WithRevitContext(() => null)
            .WriteTo.Sink(testSink)
            .CreateLogger();

        var exception = Record.Exception(() => logger.Warning("Before any Revit document is open"));

        Assert.Null(exception);
        Assert.Single(testSink.LogEvents);
    }

    [Fact]
    public void Logging_does_not_throw_when_context_factory_fails()
    {
        var testSink = new TestSink();
        using var logger = new LoggerConfiguration()
            .Enrich.WithRevitContext(() => throw new InvalidOperationException("Context is unavailable"))
            .WriteTo.Sink(testSink)
            .CreateLogger();

        var exception = Record.Exception(() => logger.Warning("Context resolution failed"));

        Assert.Null(exception);
        Assert.Single(testSink.LogEvents);
    }

    [Fact]
    public void Default_property_names_are_distinct()
    {
        var distinct = RevitPropertyNames.Distinct();

        Assert.Equal(RevitPropertyNames.Length, distinct.Count());
    }
}