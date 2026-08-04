using Autodesk.Revit.DB;
using Revit.Context.Abstractions.Services;
using Serilog.Core;
using Serilog.Events;

namespace Revit.Linter.SerilogEnrichers.Enrichers;

/// <summary>
/// Enriches log events with the model path of the currently active document.
/// </summary>
internal sealed class RevitDocumentModelPathPropertyEnricher(Func<IRevitContext?> contextFactory, string propertyName) : ILogEventEnricher
{
    private readonly Func<IRevitContext?> _contextFactory = contextFactory;
    private readonly string _propertyName = propertyName;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        string? modelPath = null;
        try
        {
            var document = _contextFactory()?.ActiveDocument;
            if (document is { IsModelInCloud: true })
            {
                modelPath = ConvertToUserVisiblePath(document.GetCloudModelPath());
            }
            else if (document is { IsWorkshared: true })
            {
                modelPath = ConvertToUserVisiblePath(document.GetWorksharingCentralModelPath());
            }
        }
        catch
        {
            // Revit API access is best-effort and must never break logging.
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(_propertyName, modelPath));
    }

    private static string? ConvertToUserVisiblePath(ModelPath modelPath)
        => ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);
}