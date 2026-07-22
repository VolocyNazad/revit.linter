using Microsoft.Extensions.Logging;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.Diagnostic.Infrastructure.Exceptions;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Services;
using Revit.Linter.ElementIgnoring.Abstractions.Services;

//using Revit.Linter.StatusBar.Services;
using System.Diagnostics;
using Toolkit.Revit.Extensions;

namespace Revit.Linter.Diagnostic.Services;

internal sealed class DiagnosticService(
        IDiagnosticReportSender diagnosticReportSender,
        IEnumerable<DocumentDiagnosticIdOverride> @override,
        IEnumerable<IDocumentDiagnostic> documentDiagnostics, IEnumerable<IDocumentDiagnosticFilter> filter,
        IEnumerable<ElementDiagnosticIdOverride> elementDiagnosticIdOverrides,
        IEnumerable<IElementDiagnostic> elementDiagnostics, IEnumerable<IElementDiagnosticFilter> elementDiagnosticFilters, 
        IEnumerable<IElementDiagnosticDocumentFilter> elementDiagnosticDocumentFilters,
        IIgnoreElementDetector ignoreElementDetector,
        ILogger<DiagnosticService> logger)
    : IDiagnosticService
{
    private readonly ElementFilter _elementFilter = ElementFilterUtils.AllFilter();
    private IList<(DocumentDiagnosticId, DocumentDiagnosticIdOverride, IDocumentDiagnosticFilter, IDocumentDiagnostic)> DocumentDiagnosticInfo
    {
        get
        {
            if (field != null) return field;

            var infos = documentDiagnostics
                .Select(diagnostic => (
                     diagnostic.Identity,
                     @override.FirstOrDefault(o => o.Identity == diagnostic.Identity)
                         ?? throw new InvalidOperationException($"Document diagnostic overrides with {diagnostic.Identity} not found."),
                     filter.First(f => f.Identity == diagnostic.Identity)
                         ?? throw new InvalidOperationException($"Document diagnostic filter with {diagnostic.Identity} not found."),
                     diagnostic
                 )).ToList();

            var documentDiagnosticIds = documentDiagnostics.Select(i => i.Identity).ToList();
            var hasDuplicates = documentDiagnosticIds.Count != new HashSet<DocumentDiagnosticId>(documentDiagnosticIds).Count;
            if (hasDuplicates) throw new DuplicateDiagnosticIdException();

            return infos;
        }
    }
    private IList<(ElementDiagnosticId, ElementDiagnosticIdOverride, IElementDiagnosticFilter, IElementDiagnosticDocumentFilter, IElementDiagnostic)> ElementDiagnosticInfo
    {
        get
        {
            if (field != null) return field;

            var infos = elementDiagnostics
                .Select(diagnostic => (
                    diagnostic.Identity,
                    diagnosticIdOverrides: elementDiagnosticIdOverrides.FirstOrDefault(o => o.Identity == diagnostic.Identity)
                        ?? throw new InvalidOperationException($"Element diagnostic overrides with {diagnostic.Identity} not found."),
                    diagnosticFilter: elementDiagnosticFilters.First(f => f.Identity == diagnostic.Identity)
                        ?? throw new InvalidOperationException($"Element diagnostic filter with {diagnostic.Identity} not found."),
                    diagnosticDocumentFilter: elementDiagnosticDocumentFilters.First(f => f.Identity == diagnostic.Identity)
                        ?? throw new InvalidOperationException($"Element diagnostic document filter with {diagnostic.Identity} not found."),
                    diagnostic
                )).ToList();

            var elementDiagnosticIds = elementDiagnostics.Select(i => i.Identity).ToList();
            var hasDuplicates = elementDiagnosticIds.Count != new HashSet<ElementDiagnosticId>(elementDiagnosticIds).Count;
            if (hasDuplicates) throw new DuplicateDiagnosticIdException();

            return infos;
        }
    }

    public DiagnosticServiceResult Execute(Document document, IEnumerable<ElementId> elementIds, View? view = null)
    {
        try
        {
            var elements = elementIds.Select(document.GetElement).ToList();
            AddDocumentDiagnostics(document);
            AddElementDiagnostics(document, elements, view);

            return DiagnosticServiceResult.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Internal error");
            return DiagnosticServiceResult.Failed;
        }
    }
    public DiagnosticServiceResult Execute(Document document, View? view = null)
    {
        try
        {
            AddDocumentDiagnostics(document);
            AddElementDiagnostics(document, view);

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
        foreach ((DocumentDiagnosticId diagnosticId, DocumentDiagnosticIdOverride diagnosticIdOverrides, IDocumentDiagnosticFilter diagnosticFilter, IDocumentDiagnostic diagnostic) in DocumentDiagnosticInfo)
        {
            if (!diagnosticIdOverrides.IsActive) continue;
            if (!diagnosticFilter.IsRelevantFor(document)) continue;
            var stopwatch = Stopwatch.StartNew();
            DiagnosticFeedback feedback = diagnostic.Execute(document);
            stopwatch.Stop();
            if (feedback.Verdict == DiagnosticVerdict.Valid) continue;
            (string, object)[] messageArgs = [
                ("duration", stopwatch.Elapsed.TotalMilliseconds), 
                ("documentTitle", document.Title)
            ];
            if (feedback.AdditionalMessageArguments is not null && feedback.AdditionalMessageArguments.Any()) {
                messageArgs = messageArgs.Concat(feedback.AdditionalMessageArguments
                    .Select(i => (i.Key, i.Value))).ToArray(); // todo Оптимизировать
            }
            DiagnosticReportMessage diagnosticReportMessage = new(diagnosticId.MessageFormat, messageArgs);
            DiagnosticReport diagnosticReport = new(
                diagnosticId.Code, 
                diagnosticIdOverrides.Severity, document, diagnosticReportMessage, document, null,
                diagnosticId.IsObsolete, diagnosticId.ObsoleteDescription);
            diagnosticReportSender.Send(diagnosticReport);
        }
    }

    private void AddElementDiagnostics(Document document, View? view)
    {
        IList<Element> elements = view is null
            ? new FilteredElementCollector(document).WherePasses(_elementFilter).ToElements()
            : new FilteredElementCollector(document, view.Id).WherePasses(_elementFilter).ToElements();
        AddElementDiagnostics(document, elements, view);
    }
    private void AddElementDiagnostics(Document document, IEnumerable<Element> elements, View? view)
    {
        //using RevitProgressBar revitProgressBar = new();
        //revitProgressBar.SetMaximumValue(ElementDiagnosticInfo.Count);

        foreach ((ElementDiagnosticId diagnosticId, ElementDiagnosticIdOverride diagnosticIdOverrides, IElementDiagnosticFilter diagnosticFilter, IElementDiagnosticDocumentFilter diagnosticDocumentFilter, IElementDiagnostic diagnostic) in ElementDiagnosticInfo)
        {
            //revitProgressBar.Increment();

            if (!diagnosticDocumentFilter.IsRelevantFor(document)) continue;
            if (!diagnosticIdOverrides.IsActive) continue;
            foreach (Element element in elements)
            {
                if (ignoreElementDetector.IsElementIgnored(diagnosticId.Code, element)) continue;
                if (!diagnosticFilter.IsRelevantFor(document, element)) continue;
                var stopwatch = Stopwatch.StartNew();
                DiagnosticFeedback feedback = diagnostic.Execute(document, view, element);
                stopwatch.Stop();
                if (feedback.Verdict == DiagnosticVerdict.Valid) continue;
                (string, object)[] messageArgs = [
                    ("duration", stopwatch.Elapsed.TotalMilliseconds), 
                    ("elementId", element.Id), 
                    ("elementName", element.Name)];
                if (feedback.AdditionalMessageArguments is not null && feedback.AdditionalMessageArguments.Any()) {
                    messageArgs = messageArgs.Concat(feedback.AdditionalMessageArguments
                        .Select(i => (i.Key, i.Value))).ToArray(); // todo Оптимизировать
                }
                object[] targetDependencies = [];
                if (feedback.AdditionalTargetDependencies is not null && feedback.AdditionalTargetDependencies.Any()) {
                    targetDependencies = targetDependencies.Concat(feedback.AdditionalTargetDependencies).ToArray(); // todo Оптимизировать
                }
                DiagnosticReportMessage diagnosticReportMessage = new(
                    diagnosticId.MessageFormat, messageArgs);
                DiagnosticReport diagnosticReport = new(
                    diagnosticId.Code, diagnosticIdOverrides.Severity, 
                    document, diagnosticReportMessage, element, targetDependencies, diagnosticId.IsObsolete, diagnosticId.ObsoleteDescription);
                diagnosticReportSender.Send(diagnosticReport);
            }
        }

    }
}
