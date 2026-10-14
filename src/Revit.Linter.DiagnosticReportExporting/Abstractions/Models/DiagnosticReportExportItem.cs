namespace Revit.Linter.DiagnosticReportExporting.Abstractions.Models;

/// <summary>
/// One displayed finding prepared for export.
/// </summary>
/// <param name="Severity">The stable severity name (<c>Error</c>, <c>Warning</c> or <c>Message</c>).</param>
/// <param name="SeverityDisplayName">The localized severity name shown to the user.</param>
/// <param name="Code">The stable diagnostic code.</param>
/// <param name="Message">The rendered finding message.</param>
/// <param name="Document">The title of the document the finding belongs to.</param>
/// <param name="Created">The moment the finding was reported.</param>
/// <param name="IsObsolete">Whether the finding is obsolete.</param>
/// <param name="ObsoleteDescription">The reason the finding is obsolete, or <see langword="null"/>.</param>
/// <param name="TargetElementId">The target element identifier text, or <see langword="null"/> when unknown.</param>
/// <param name="DependencyElementIds">The dependency element identifier texts.</param>
public sealed record DiagnosticReportExportItem(
    string Severity,
    string SeverityDisplayName,
    string Code,
    string Message,
    string Document,
    DateTimeOffset Created,
    bool IsObsolete,
    string? ObsoleteDescription,
    string? TargetElementId,
    IReadOnlyCollection<string> DependencyElementIds);
