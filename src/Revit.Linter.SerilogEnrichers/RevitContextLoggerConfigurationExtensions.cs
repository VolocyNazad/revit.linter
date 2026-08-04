using Revit.Context.Abstractions.Services;
using Revit.Linter.SerilogEnrichers.Enrichers;
using Revit.Linter.SerilogEnrichers.Utils;
using Serilog;
using Serilog.Configuration;

namespace Revit.Linter.SerilogEnrichers;

/// <summary>
/// Extends <see cref="LoggerEnrichmentConfiguration"/> with Autodesk Revit context properties.
/// <para/>
/// Based on the MIT-licensed <see href="https://github.com/dosymep/Serilog.Enrichers.Autodesk.Revit"/>
/// project, adapted to the <see cref="IRevitContext"/> abstraction resolved lazily at log time.
/// </summary>
public static class RevitContextLoggerConfigurationExtensions
{
    /// <summary>
    /// Default revit version property name.
    /// </summary>
    public const string RevitVersionPropertyName = RevitPropertyNames.Version;

    /// <summary>
    /// Default revit build property name.
    /// </summary>
    public const string RevitBuildPropertyName = RevitPropertyNames.Build;

    /// <summary>
    /// Default revit username property name.
    /// </summary>
    public const string RevitUserNamePropertyName = RevitPropertyNames.UserName;

    /// <summary>
    /// Default revit language property name.
    /// </summary>
    public const string RevitLanguagePropertyName = RevitPropertyNames.Language;

    /// <summary>
    /// Default revit add-in property name.
    /// </summary>
    public const string RevitAddinIdPropertyName = RevitPropertyNames.AddinId;

    /// <summary>
    /// Default revit document title property name.
    /// </summary>
    public const string RevitDocumentTitlePropertyName = RevitPropertyNames.DocumentTitle;

    /// <summary>
    /// Default revit document path name property name.
    /// </summary>
    public const string RevitDocumentPathNamePropertyName = RevitPropertyNames.DocumentPathName;

    /// <summary>
    /// Default revit document model path property name.
    /// </summary>
    public const string RevitDocumentModelPathPropertyName = RevitPropertyNames.DocumentModelPath;

    /// <summary>
    /// Enrich log events with all Revit context properties: version, build, user name,
    /// language, add-in id, current document title, path name and model path.
    /// The <paramref name="revitContextFactory"/> is invoked lazily and only until the Revit context
    /// becomes available, after which it is cached.
    /// </summary>
    /// <param name="loggerEnrichmentConfiguration">Logger enrichment configuration.</param>
    /// <param name="revitContextFactory">Factory returning the current <see cref="IRevitContext"/> or <c>null</c>.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    public static LoggerConfiguration WithRevitContext(
        this LoggerEnrichmentConfiguration loggerEnrichmentConfiguration,
        Func<IRevitContext?> revitContextFactory) =>
        loggerEnrichmentConfiguration.With(new RevitContextEnricher(revitContextFactory));

    /// <summary>
    /// Enrich log events with a <c>Application.VersionNumber</c> property.
    /// </summary>
    public static LoggerConfiguration WithRevitVersion(
        this LoggerEnrichmentConfiguration loggerEnrichmentConfiguration,
        Func<IRevitContext?> revitContextFactory,
        string revitVersionPropertyName = RevitVersionPropertyName) =>
        loggerEnrichmentConfiguration.With(new RevitStaticPropertyEnricher(
            revitContextFactory,
            context => context.ControlledApplication is { } application && int.TryParse(application.VersionNumber, out int version) ? version : null,
            revitVersionPropertyName));

    /// <summary>
    /// Enrich log events with a <c>Application.VersionBuild</c> property.
    /// </summary>
    public static LoggerConfiguration WithRevitBuild(
        this LoggerEnrichmentConfiguration loggerEnrichmentConfiguration,
        Func<IRevitContext?> revitContextFactory,
        string revitBuildPropertyName = RevitBuildPropertyName) =>
        loggerEnrichmentConfiguration.With(new RevitStaticPropertyEnricher(
            revitContextFactory,
            context => context.ControlledApplication is { } application ? application.VersionBuild : null,
            revitBuildPropertyName));

    /// <summary>
    /// Enrich log events with a <c>Application.Username</c> property.
    /// </summary>
    public static LoggerConfiguration WithRevitUserName(
        this LoggerEnrichmentConfiguration loggerEnrichmentConfiguration,
        Func<IRevitContext?> revitContextFactory,
        string revitUserNamePropertyName = RevitUserNamePropertyName) =>
        loggerEnrichmentConfiguration.With(new RevitStaticPropertyEnricher(
            revitContextFactory,
            context => context.Application is { } application ? application.Username : null,
            revitUserNamePropertyName));

    /// <summary>
    /// Enrich log events with a <c>Application.Language</c> property.
    /// </summary>
    public static LoggerConfiguration WithRevitLanguage(
        this LoggerEnrichmentConfiguration loggerEnrichmentConfiguration,
        Func<IRevitContext?> revitContextFactory,
        string revitLanguagePropertyName = RevitLanguagePropertyName) =>
        loggerEnrichmentConfiguration.With(new RevitStaticPropertyEnricher(
            revitContextFactory,
            context => context.ControlledApplication is { } application ? application.Language.GetCultureInfo().EnglishName : null,
            revitLanguagePropertyName));

    /// <summary>
    /// Enrich log events with a current add-in id property.
    /// </summary>
    public static LoggerConfiguration WithRevitAddinId(
        this LoggerEnrichmentConfiguration loggerEnrichmentConfiguration,
        Func<IRevitContext?> revitContextFactory,
        string revitAddinPropertyName = RevitAddinIdPropertyName) =>
        loggerEnrichmentConfiguration.With(new RevitAddinIdPropertyEnricher(revitContextFactory, revitAddinPropertyName));

    /// <summary>
    /// Enrich log events with a current <c>Document.Title</c> property.
    /// </summary>
    public static LoggerConfiguration WithRevitDocumentTitle(
        this LoggerEnrichmentConfiguration loggerEnrichmentConfiguration,
        Func<IRevitContext?> revitContextFactory,
        string revitDocumentTitlePropertyName = RevitDocumentTitlePropertyName) =>
        loggerEnrichmentConfiguration.With(new RevitDocumentTitlePropertyEnricher(revitContextFactory, revitDocumentTitlePropertyName));

    /// <summary>
    /// Enrich log events with a current <c>Document.PathName</c> property.
    /// </summary>
    public static LoggerConfiguration WithRevitDocumentPathName(
        this LoggerEnrichmentConfiguration loggerEnrichmentConfiguration,
        Func<IRevitContext?> revitContextFactory,
        string revitDocumentPathNamePropertyName = RevitDocumentPathNamePropertyName) =>
        loggerEnrichmentConfiguration.With(new RevitDocumentPathNamePropertyEnricher(revitContextFactory, revitDocumentPathNamePropertyName));

    /// <summary>
    /// Enrich log events with a current document model path.
    /// </summary>
    public static LoggerConfiguration WithRevitDocumentModelPath(
        this LoggerEnrichmentConfiguration loggerEnrichmentConfiguration,
        Func<IRevitContext?> revitContextFactory,
        string revitDocumentModelPathPropertyName = RevitDocumentModelPathPropertyName) =>
        loggerEnrichmentConfiguration.With(new RevitDocumentModelPathPropertyEnricher(revitContextFactory, revitDocumentModelPathPropertyName));
}