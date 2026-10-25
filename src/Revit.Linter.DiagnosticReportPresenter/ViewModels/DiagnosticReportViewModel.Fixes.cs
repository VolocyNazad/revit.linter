// Fix creation and execution, ignore fixes and transaction helpers.

using Revit.Linter.ThemeManaging.Controls;
using Microsoft.Extensions.Logging;
using Revit.Async;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DiagnosticReportProvider.Abstractions.Models;
using Revit.Linter.ElementIgnoring.Abstractions.Models;
using Revit.Linter.FixReportProvider.Abstractions.Models;
using System.Text;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

internal sealed partial class DiagnosticReportViewModel
{
    private List<FixViewModel> CreateFixes(DiagnosticReport report)
    {
        if (report.Target is Element element)
        {
            var doc = report.Document;
            if (doc is not { IsValidObject: true }) return [];

            // Fixes are built lazily when the UI first shows the row, by which time the target
            // may have been deleted or undone; reading Id on such an element throws.
            ElementId? maybeElementId = TryGetElementId(element);
            if (maybeElementId is null)
            {
                _logger.LogDebug(
                    "Fixes are unavailable for diagnostic {DiagnosticCode}: target element is no longer valid",
                    report.Code);
                return [];
            }
            ElementId elementId = maybeElementId;

            PathIcon icon = CreateLightbulbIcon();
            FixViewModel ignoreFix = new()
            {
                Icon = icon,
                Title = IgnoreFixTitle,
                FixDelegate = async (cancellationToken) =>
                {
                    if (doc is null or { IsValidObject: false }) return;

                    if (!element.IsValidObject) return;

                    string? feedbackMessage = null;
                    string transactionName = GetLocalizedString("ignoreElement_transaction");
                    bool success = await ExecuteTransaction(doc, transactionName, () => {
                            var feedback = _ignoreElementProvider.Ignore(report.Code, element);
                            feedbackMessage = feedback.Message;
                            return feedback.Result == IgnoreElementResult.Success;
                        }, cancellationToken);

                    string message = !success
                        ? IgnoreElementFailedMessage + Environment.NewLine
                            + GetLocalizedString("details_message", feedbackMessage ?? string.Empty)
                        : IgnoreElementSucceededMessage;
                    _fixReportSender.Send(new FixReport(
                        report.Code, report.Document.Title, new(message, ("elementId", elementId))));
                    if (success)
                    {
                        ReportIgnoreApplied(report.Code);
                        // The ignore guarantees the next run will not report this finding, and the
                        // ignored element itself is untouched, so no change event removes the stale row.
                        RemoveIgnoredRows(report.Code, doc.Title, [elementId]);
                    }

                    if (!success)
                    {
                        string resolvedMessage = message.Replace("{elementId}", elementId.ToString());
                        await _dialog.Show(new DialogRequest(resolvedMessage), cancellationToken);
                    }
                }
            };

            icon = CreateLightbulbIcon();
            async Task RunIgnoreFixAll(CancellationToken cancellationToken)
            {
                if (doc is null or { IsValidObject: false }) return;

                if (!element.IsValidObject) return;

                StringBuilder feedbackMessage = new();
                bool hasErrors = false;
                string transactionName = GetLocalizedString("ignoreElements_transaction");

                // The targets are collected before the transaction: the provider call is atomic, so
                // a failure rolls nothing back, and a success makes every collected row stale at once.
                List<(Element Element, ElementId Id)> targets = [];
                foreach (DiagnosticReportItemViewModel reportItem in Collection)
                {
                    if (reportItem.Code != report.Code) continue;
                    if (reportItem.Target is not Element targetElement) continue;
                    if (TryGetElementId(targetElement) is not { } rowId) continue;
                    targets.Add((targetElement, rowId));
                }

                await ExecuteTransaction(doc, transactionName, () => {
                        try
                        {
                            var feedback = _ignoreElementProvider.IgnoreMany(
                                report.Code, targets.Select(target => target.Element));
                            if (!string.IsNullOrEmpty(feedback.Message))
                            {
                                feedbackMessage.Append(feedback.Message);
                                feedbackMessage.Append(Environment.NewLine);
                            }
                            if (feedback.Result == IgnoreElementResult.Failed) hasErrors = true;
                        }
                        catch (Exception ex)
                        {
                            hasErrors = true;
                            _logger.LogDebug(
                                ex,
                                "Failed to ignore elements for diagnostic {DiagnosticCode}",
                                report.Code);
                            feedbackMessage.Append(ex.Message);
                            feedbackMessage.Append(Environment.NewLine);
                        }
                        return true;
                    }, cancellationToken);

                string message = GetLocalizedString(hasErrors
                    ? "ignoreElementsFailed_message"
                    : "ignoreElementsSucceeded_message");
                _fixReportSender.Send(new FixReport(
                    report.Code, report.Document.Title, new(message, ("elementId", elementId))));
                if (!hasErrors)
                {
                    ReportIgnoreApplied(report.Code);
                    RemoveIgnoredRows(
                        report.Code, doc.Title, targets.Select(target => target.Id));
                }

                if (hasErrors)
                {
                    string dialogMessage = message + Environment.NewLine
                        + GetLocalizedString("details_message", feedbackMessage.ToString());

                    bool retry = await _confirmationDialog.Show(
                        new ConfirmationDialogRequest(dialogMessage, RetryTitle), cancellationToken);

                    // Already-ignored elements will disappear from the report (the diagnostic will no
                    // longer trigger for them), so a retry will only affect the remaining problem elements.
                    if (retry) await RunIgnoreFixAll(cancellationToken);
                }
            }

            FixViewModel ignoreFixAll = new()
            {
                Icon = icon,
                Title = GetLocalizedString("all_title", IgnoreFixTitle),
                FixDelegate = RunIgnoreFixAll
            };

            return GetCatalogSnapshot().ElementDiagnostics.SelectMany(registration => registration.Fixes)
                .Where(i => i.Identity.Code == report.Code)
                .SelectMany(i =>
                {
                    List<FixViewModel> fixes = [];

                    PathIcon icon = CreateLightbulbIcon();
                    FixViewModel fix = new() {
                        Icon = icon,
                        Title = i.Value,
                        FixDelegate = async (cancellationToken) => {
                            if (doc is null or { IsValidObject: false }) return;

                            if (!element.IsValidObject) return;

                            string transactionName = i.Value;
                            (bool success, string? error) = await ExecuteTransactionWithError(
                                doc, transactionName, () => ExecuteFix(
                                    i, CreateFixContext(report.Document, report.TargetDependencies, element), element),
                                cancellationToken);

                            string message = GetLocalizedString(success
                                ? "fixElementSucceeded_message"
                                : "fixElementFailed_message");
                            _fixReportSender.Send(new FixReport(
                                i.Identity.Code, report.Document.Title, new(message, ("elementId", elementId))));
                            if (success) ReportFixApplied(report.Code);

                            if (!success)
                            {
                                string resolvedMessage = message.Replace("{elementId}", elementId.ToString());
                                string dialogMessage = string.IsNullOrWhiteSpace(error)
                                    ? resolvedMessage
                                    : resolvedMessage + Environment.NewLine
                                        + GetLocalizedString("details_message", error ?? string.Empty);
                                await _dialog.Show(new DialogRequest(dialogMessage), cancellationToken);
                            }
                        }
                    };
                    fixes.Add(fix);

                    icon = CreateLightbulbIcon();
                    async Task RunFixAll(CancellationToken cancellationToken)
                    {
                        if (doc is null or { IsValidObject: false }) return;

                        StringBuilder errors = new();
                        bool hasErrors = false;
                        string transactionName = GetLocalizedString("all_title", i.Value);

                        // The transaction is always committed (we return true) so that successfully fixed
                        // elements are not rolled back just because some elements could not be fixed.
                        await ExecuteTransaction(doc, transactionName, () => {
                                foreach (var reportItem in Collection)
                                {
                                    if (reportItem.Code != report.Code) continue;
                                    if (reportItem.Target is not Element element) continue;

                                    ElementId? rowElementId = TryGetElementId(element);
                                    if (rowElementId is null) continue;

                                    try
                                    {
                                        bool result = ExecuteFix(
                                            i, CreateFixContext(doc, reportItem.TargetDependencies, element), element);
                                        if (!result)
                                        {
                                            hasErrors = true;
                                            errors.AppendLine(GetLocalizedString(
                                                "fixElementFailed_message") + $" (id: {rowElementId})");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        hasErrors = true;
                                        _logger.LogDebug(
                                            ex,
                                            "Bulk fix {FixCode} failed for element {ElementId} of diagnostic {DiagnosticCode}",
                                            i.Identity.Code,
                                            rowElementId,
                                            report.Code);
                                        errors.AppendLine($"id {rowElementId}: {ex.Message}");
                                    }
                                }
                                return true;
                            }, cancellationToken);

                        string message = GetLocalizedString(hasErrors
                            ? "fixElementsFailed_message"
                            : "fixElementsSucceeded_message");
                        _fixReportSender.Send(new FixReport(i.Identity.Code, report.Document.Title, new(message)));
                        if (!hasErrors) ReportFixApplied(report.Code);

                        if (hasErrors)
                        {
                            string dialogMessage = message + Environment.NewLine
                                + GetLocalizedString("details_message", errors.ToString());

                            bool retry = await _confirmationDialog.Show(
                                new ConfirmationDialogRequest(dialogMessage, RetryTitle), cancellationToken);

                            // Already-fixed elements have become invalid (IsValidObject == false) and will
                            // be skipped on the next pass — a retry will only affect the remaining problem elements.
                            if (retry) await RunFixAll(cancellationToken);
                        }
                    }

                    FixViewModel fixAll = new() {
                        Icon = icon,
                        Title = GetLocalizedString("all_title", i.Value),
                        FixDelegate = RunFixAll
                    };
                    fixes.Add(fixAll);

                    return fixes;
                }).Append(ignoreFix).Append(ignoreFixAll).ToList();
        }
        else if (report.Target is Document document)
        {
            if (document is not { IsValidObject: true }) return [];
            string documentTitle = document.Title;
            return GetCatalogSnapshot().DocumentDiagnostics.SelectMany(registration => registration.Fixes)
                .Where(i => i.Identity.Code == report.Code)
                .Select(i =>
                {
                    PathIcon icon = CreateLightbulbIcon();
                    FixViewModel fix = new() {
                        Icon = icon,
                        Title = i.Value,
                        FixDelegate = async (cancellationToken) => {
                            if (document is null or { IsValidObject: false }) return;

                            string transactionName = i.Value;
                            (bool success, string? error) = await ExecuteTransactionWithError(
                                document, transactionName, () => i.Execute(document), cancellationToken);

                            string message = GetLocalizedString(success
                                ? "fixDocumentSucceeded_message"
                                : "fixDocumentFailed_message");
                            _fixReportSender.Send(new FixReport(
                                i.Identity.Code, report.Document.Title,
                                new(message, ("documentTitle", documentTitle))));
                            if (success) ReportFixApplied(report.Code);

                            if (!success)
                            {
                                string resolvedMessage = message.Replace("{documentTitle}", documentTitle);
                                string dialogMessage = string.IsNullOrWhiteSpace(error)
                                    ? resolvedMessage
                                    : resolvedMessage + Environment.NewLine
                                        + GetLocalizedString("details_message", error ?? string.Empty);
                                await _dialog.Show(new DialogRequest(dialogMessage), cancellationToken);
                            }
                        }
                    };
                    return fix;
                }).Append(CreateDocumentIgnoreFix(report, document, documentTitle)).ToList();
        }
        return [];
    }

    /// <summary>
    /// Creates the lightbulb icon that marks a fix menu, colored by the current theme.
    /// </summary>
    /// <returns>An icon whose foreground follows the <c>LinterHighlightIconBrush</c> resource.</returns>
    /// <remarks>
    /// The icon is created for every fix row, so the brush is taken through a resource reference instead
    /// of a hard-coded color: a theme switch repaints the icon without recreating the rows.
    /// </remarks>
    private static PathIcon CreateLightbulbIcon()
    {
        PathIcon icon = new() { Kind = IconKind.Lightbulb };
        icon.SetResourceReference(PathIcon.ForegroundProperty, "LinterHighlightIconBrush");
        return icon;
    }

    private FixViewModel CreateDocumentIgnoreFix(DiagnosticReport report, Document document, string documentTitle)
    {
        PathIcon icon = CreateLightbulbIcon();
        return new()
        {
            Icon = icon,
            Title = IgnoreFixTitle,
            FixDelegate = async (cancellationToken) =>
            {
                if (document is not { IsValidObject: true }) return;

                string? feedbackMessage = null;
                string transactionName = GetLocalizedString("ignoreDocument_transaction");
                bool success = await ExecuteTransaction(document, transactionName, () => {
                        var feedback = _ignoreElementProvider.IgnoreDocument(report.Code, document);
                        feedbackMessage = feedback.Message;
                        return feedback.Result == IgnoreElementResult.Success;
                    }, cancellationToken);

                string message = !success
                    ? IgnoreDocumentFailedMessage + Environment.NewLine
                        + GetLocalizedString("details_message", feedbackMessage ?? string.Empty)
                    : IgnoreDocumentSucceededMessage;
                _fixReportSender.Send(new FixReport(
                    report.Code, report.Document.Title, new(message, ("documentTitle", documentTitle))));
                if (success)
                {
                    ReportIgnoreApplied(report.Code);
                    // A document-level ignore suppresses every finding of the code in this document,
                    // so all of its rows are stale, and no change event removes them.
                    RemoveIgnoredDocumentRows(report.Code, documentTitle);
                }

                if (!success)
                {
                    string resolvedMessage = message.Replace("{documentTitle}", documentTitle);
                    await _dialog.Show(new DialogRequest(resolvedMessage), cancellationToken);
                }
            }
        };
    }

    private void ReportFixApplied(string diagnosticCode)
        => _activityStream.Publish(new FixAppliedActivity(diagnosticCode));

    private void ReportIgnoreApplied(string diagnosticCode)
        => _activityStream.Publish(new IgnoreAppliedActivity(diagnosticCode));

    /// <summary>
    /// Removes the report rows the ignore just made stale: their findings will no longer be produced.
    /// </summary>
    /// <param name="code">The ignored diagnostic code.</param>
    /// <param name="documentTitle">The title of the document the ignore was written to.</param>
    /// <param name="ignoredIds">The ignored element identifiers.</param>
    private void RemoveIgnoredRows(string code, string documentTitle, IEnumerable<ElementId> ignoredIds)
    {
        HashSet<ElementId> ids = [.. ignoredIds];
        List<DiagnosticReportItemViewModel> stale = Collection
            .Where(item => string.Equals(item.Code, code, StringComparison.Ordinal)
                && string.Equals(item.DocumentTitle, documentTitle, StringComparison.Ordinal)
                && item.TargetElementId is { } targetId
                && ids.Contains(targetId))
            .ToList();
        foreach (DiagnosticReportItemViewModel row in stale)
            Collection.Remove(row);
        if (stale.Count > 0)
            _logger.LogDebug(
                "Removed {RowCount} ignored rows of diagnostic {DiagnosticCode}",
                stale.Count, code);
    }

    /// <summary>
    /// Removes every report row of a document-level ignore: the whole code is suppressed in the document.
    /// </summary>
    /// <param name="code">The ignored diagnostic code.</param>
    /// <param name="documentTitle">The title of the ignored document.</param>
    private void RemoveIgnoredDocumentRows(string code, string documentTitle)
    {
        List<DiagnosticReportItemViewModel> stale = Collection
            .Where(item => string.Equals(item.Code, code, StringComparison.Ordinal)
                && string.Equals(item.DocumentTitle, documentTitle, StringComparison.Ordinal))
            .ToList();
        foreach (DiagnosticReportItemViewModel row in stale)
            Collection.Remove(row);
        if (stale.Count > 0)
            _logger.LogDebug(
                "Removed {RowCount} ignored rows of diagnostic {DiagnosticCode}",
                stale.Count, code);
    }

    private static ElementFixContext CreateFixContext(
        Document document,
        object[]? dependencies,
        Element target)
    {
        ElementId[] dependencyIds = dependencies?
            .OfType<Element>()
            .Where(dependency => dependency.IsValidObject && dependency.Document.Equals(document))
            .Select(dependency => dependency.Id)
            .ToArray() ?? [];
        var elementSets = new Dictionary<string, IReadOnlyCollection<ElementId>>
        {
            [ElementVisualizationSetKeys.Target] = [target.Id],
            [ElementVisualizationSetKeys.Dependencies] = dependencyIds
        };
        return new(document, elementSets);
    }

    private static bool ExecuteFix(IElementFix fix, ElementFixContext context, Element target) =>
        fix is IElementSetFix elementSetFix
            ? elementSetFix.Execute(context)
            : fix.Execute(target);

    private static async Task<bool> ExecuteTransaction(
        Document document, string transactionName, Func<bool> action, CancellationToken cancellationToken)
    {
        (bool success, _) = await ExecuteTransactionWithError(document, transactionName, action, cancellationToken);
        return success;
    }

    private static Task<(bool Success, string? Error)> ExecuteTransactionWithError(
        Document document, string transactionName, Func<bool> action, CancellationToken cancellationToken)
        => RevitTask.RunAsync(_ => {
            cancellationToken.ThrowIfCancellationRequested();

            using Transaction transaction = new(document, transactionName);
            try
            {
                if (transaction.Start() != TransactionStatus.Started)
                    return (false, (string?)GetLocalizedString("transactionStartFailed_message"));
                if (!action())
                    return (false, (string?)GetLocalizedString("operationFailed_message"));

                return (transaction.Commit() == TransactionStatus.Committed, (string?)null);
            }
            catch (Exception ex)
            {
                return (false, (string?)ex.Message);
            }
        });
}

