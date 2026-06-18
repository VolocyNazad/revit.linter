using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Diagnostic.Infrastructure.Exceptions;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using System.Diagnostics;

namespace Revit.Linter.Diagnostic.Services;

internal sealed class DiagnosticService(
        IDiagnosticReportSender diagnosticReportSender,
        IEnumerable<DocumentDiagnosticId> documentDiagnosticIds,
        IEnumerable<DocumentDiagnosticIdOverrides> documentDiagnosticIdOverrides,
        IEnumerable<IDocumentDiagnostic> documentDiagnostics, IEnumerable<IDocumentDiagnosticFilter> documentDiagnosticFilters,
        IEnumerable<ElementDiagnosticId> elementDiagnosticIds,
        IEnumerable<ElementDiagnosticIdOverrides> elementDiagnosticIdOverrides,
        IEnumerable<IElementDiagnostic> elementDiagnostics, IEnumerable<IElementDiagnosticFilter> elementDiagnosticFilters, 
        IEnumerable<IElementDiagnosticDocumentFilter> elementDiagnosticDocumentFilters,
        ILogger<DiagnosticService> logger)
    : IDiagnosticService
{
    private readonly ElementFilter elementFilter = new LogicalOrFilter(
        new ElementIsElementTypeFilter(true), new ElementIsElementTypeFilter(false));
    private IList<(DocumentDiagnosticId, DocumentDiagnosticIdOverrides, IDocumentDiagnosticFilter, IDocumentDiagnostic)> DocumentDiagnosticInfo
    {
        get
        {
            if (field != null) return field;

            var infos = documentDiagnosticIds
                   .Select(id => (
                        id,
                        documentDiagnosticIdOverrides.FirstOrDefault(o => o.Identity == id)
                            ?? throw new InvalidOperationException($"Document diagnostic overrides with {id} not found."),
                        documentDiagnosticFilters.First(f => f.Identity == id)
                            ?? throw new InvalidOperationException($"Document diagnostic filter with {id} not found."),
                        documentDiagnostics.First(d => d.Identity == id)
                            ?? throw new InvalidOperationException($"Document diagnostic with {id} not found.")
                    )).ToList();

            var hasDuplicates = documentDiagnosticIds.Count() != new HashSet<DocumentDiagnosticId>(documentDiagnosticIds).Count;
            if (hasDuplicates) throw new DuplicateDiagnosticIdException();
            // todo Проверить на дубликаты фильтры и сервисы диагностики

            return infos;
        }
    }
    private IList<(ElementDiagnosticId, ElementDiagnosticIdOverrides, IElementDiagnosticFilter, IElementDiagnosticDocumentFilter, IElementDiagnostic)> ElementDiagnosticInfo
    {
        get
        {
            if (field != null) return field;

            var infos = elementDiagnosticIds
                    .Select(id => (
                        id,
                        diagnosticIdOverrides: elementDiagnosticIdOverrides.FirstOrDefault(o => o.Identity == id)
                            ?? throw new InvalidOperationException($"Element diagnostic overrides with {id} not found."),
                        diagnosticFilter: elementDiagnosticFilters.First(f => f.Identity == id)
                            ?? throw new InvalidOperationException($"Element diagnostic filter with {id} not found."),
                        diagnosticDocumentFilter: elementDiagnosticDocumentFilters.First(f => f.Identity == id)
                            ?? throw new InvalidOperationException($"Element diagnostic document filter with {id} not found."),
                        diagnostic: elementDiagnostics.First(d => d.Identity == id)
                            ?? throw new InvalidOperationException($"Element diagnostic with {id} not found.")
                    )).ToList();

            var hasDuplicates = elementDiagnosticIds.Count() != new HashSet<ElementDiagnosticId>(elementDiagnosticIds).Count;
            if (hasDuplicates) throw new DuplicateDiagnosticIdException();
            // todo Проверить на дубликаты фильтры и сервисы диагностики

            return infos;
        }
    }

    public DiagnosticServiceResult Excecute(Document document, IEnumerable<ElementId> elementIds, View? view)
    {
        try
        {
            var elements = elementIds.Select(document.GetElement).ToList();
            AddElementtDiagnostics(document, elements, view);

            return DiagnosticServiceResult.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Internal error");
            return DiagnosticServiceResult.Failed;
        }
    }
    public DiagnosticServiceResult Excecute(Document document, View? view = null)
    {
        try
        {
            AddHandleFailures(document); // todo Учитывать вид
            AddDocumentDiagnostics(document);
            AddElementtDiagnostics(document, view);

            return DiagnosticServiceResult.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Internal error");
            return DiagnosticServiceResult.Failed;
        }
    }
    private void AddDocumentDiagnostics(Document document)
    {
        foreach ((DocumentDiagnosticId diagnosticId, DocumentDiagnosticIdOverrides diagnosticIdOverrides, IDocumentDiagnosticFilter diagnosticFilter, IDocumentDiagnostic diagnostic) in DocumentDiagnosticInfo)
        {
            if (!diagnosticIdOverrides.IsActive) continue;
            if (!diagnosticFilter.IsRelevantFor(document)) continue;
            var stopwatch = Stopwatch.StartNew();
            DiagnosticResult diagnosticResult = diagnostic.Execute(document);
            stopwatch.Stop();
            if (diagnosticResult.Verdict == DiagnosticVerdict.Valid) continue;
            (string, object)[] messageArgs;
            if (diagnosticResult.MessageArgs is not null) {
                messageArgs = diagnosticResult.MessageArgs
                    .Select(i => (i.Key, i.Value))
                    .Append(("duration", stopwatch.Elapsed.TotalMilliseconds))
                    .Append(("documentTitle", document.Title)).ToArray(); // todo Оптимизировать
            }
            else {
                messageArgs = [("duration", stopwatch.Elapsed.TotalMilliseconds), ("documentTitle", document.Title)];
            }
            DiagnosticReportMessage diagnosticReportMessage = new(diagnosticId.MessageFormat, messageArgs);
            DiagnosticReport diagnosticReport = new(diagnosticId.Code, diagnosticIdOverrides.Severity, document, document, diagnosticReportMessage, diagnosticId.IsObsolete, diagnosticId.ObsoleteDescription);
            diagnosticReportSender.Send(diagnosticReport);
        }
    }

    private void AddElementtDiagnostics(Document document, View? view)
    {
        IList<Element> elements = view is null
            ? new FilteredElementCollector(document).WherePasses(elementFilter).ToElements()
            : new FilteredElementCollector(document, view.Id).WherePasses(elementFilter).ToElements();
        AddElementtDiagnostics(document, elements, view);
    }
    private void AddElementtDiagnostics(Document document, IEnumerable<Element> elements, View? view)
    {
        foreach ((ElementDiagnosticId diagnosticId, ElementDiagnosticIdOverrides diagnosticIdOverrides, IElementDiagnosticFilter diagnosticFilter, IElementDiagnosticDocumentFilter diagnosticDocumentFilter, IElementDiagnostic diagnostic) in ElementDiagnosticInfo)
        {
            if (!diagnosticDocumentFilter.IsRelevantFor(document)) continue;
            foreach (Element element in elements)
            {
                if (!diagnosticIdOverrides.IsActive) continue;
                if (!diagnosticFilter.IsRelevantFor(document, element)) continue;
                var stopwatch = Stopwatch.StartNew();
                DiagnosticResult diagnosticResult = diagnostic.Execute(document, view, element);
                stopwatch.Stop();
                if (diagnosticResult.Verdict == DiagnosticVerdict.Valid) continue;
                (string, object)[] messageArgs;
                if (diagnosticResult.MessageArgs is not null) {
                    messageArgs = diagnosticResult.MessageArgs
                        .Select(i => (i.Key, i.Value))
                        .Append(("duration", stopwatch.Elapsed.TotalMilliseconds))
                        .Append(("elementId", element.Id))
                        .Append(("elementName", element.Name)).ToArray(); // todo Оптимизировать
                }
                else {
                    messageArgs = [("duration", stopwatch.Elapsed.TotalMilliseconds), ("elementId", element.Id), ("elementName", element.Name)];
                }
                DiagnosticReportMessage diagnosticReportMessage = new(
                    diagnosticId.MessageFormat, messageArgs);
                DiagnosticReport diagnosticReport = new(
                    diagnosticId.Code, diagnosticIdOverrides.Severity, document, element, diagnosticReportMessage, diagnosticId.IsObsolete, diagnosticId.ObsoleteDescription);
                diagnosticReportSender.Send(diagnosticReport);
            }
        }
    }
    private void AddHandleFailures(Document document)
    {
        foreach (FailureMessage failureMessage in document.GetWarnings())
        {
            var failureSeverity = failureMessage.GetSeverity();
            DiagnosticSeverity severity = failureSeverity switch
            {
                FailureSeverity.None => DiagnosticSeverity.Message,
                FailureSeverity.DocumentCorruption => DiagnosticSeverity.Message,
                FailureSeverity.Warning => DiagnosticSeverity.Warning,
                FailureSeverity.Error => DiagnosticSeverity.Error,
                _ => throw new NotImplementedException(
                    $"{nameof(FailureSeverity)} contains not mapped with {nameof(DiagnosticSeverity)} variant"),
            };
            DiagnosticReportMessage diagnosticReportMessage = new(
                """
                    В документе с наименованием '{documentTitle}' обнаружены предупреждения. 
                    Элементы: {elementids}
                    Детали: {details}
                    """,
                ("documentTitle", document.Title),
                ("elementids", failureMessage.GetFailingElements()),
                ("details", failureMessage.GetDescriptionText()));
            DiagnosticReport diagnosticReport = new("RVT", severity, document, null, diagnosticReportMessage, false);
            diagnosticReportSender.Send(diagnosticReport);
        }
    }
}
