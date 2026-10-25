using Revit.Linter.PracticalTourPresenter.Interactions.Abstractions.Models;

namespace Revit.Linter.PracticalTourPresenter.Infrastructure.Services;

/// <summary>
/// Advances the practical tour without observing Revit, WPF controls or diagnostic implementations.
/// </summary>
/// <remarks>
/// A host adapter translates existing application events into calls to this state machine. Only an observed
/// application action advances the tour; the tour never invokes the action being explained.
/// </remarks>
internal sealed class PracticalTourStateMachine
{
    private static readonly PracticalTourStep[] Steps =
    [
        PracticalTourStep.OpenDocument,
        PracticalTourStep.OpenConfigurationFolder,
        PracticalTourStep.SearchAndFilters,
        PracticalTourStep.SelectDiagnostic,
        PracticalTourStep.RunDiagnostics,
        PracticalTourStep.InspectFinding,
        PracticalTourStep.OpenHelp,
        PracticalTourStep.ShowElement,
        PracticalTourStep.SelectVisualization,
        PracticalTourStep.NavigateFindings,
        PracticalTourStep.UnderstandFix,
        PracticalTourStep.FixList,
        PracticalTourStep.UnderstandIgnore,
        PracticalTourStep.IgnoreList,
        PracticalTourStep.ExportReport,
        PracticalTourStep.ExportReportAnotherFormat,
        PracticalTourStep.Completed,
    ];

    /// <summary>Gets the current tour step.</summary>
    public PracticalTourStep CurrentStep { get; private set; } = PracticalTourStep.Completed;

    /// <summary>Gets a value indicating whether a practical tour session is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets a value indicating that finding-specific steps were skipped after a clean run.</summary>
    public bool HasCleanRun { get; private set; }

    /// <summary>Gets a value indicating that the inspected finding offers no visualization choice.</summary>
    public bool HasSingleVisualization { get; private set; }

    /// <summary>Gets the export format of the first tour report export, if any.</summary>
    public string? FirstExportFormat { get; private set; }

    private bool _hasSearchText;
    private bool _hasFilterUse;

    /// <summary>Starts a new session at the action appropriate to the current document state.</summary>
    public void Start(
        bool hasOpenDocument,
        PracticalTourStep? resumeStep = null,
        bool hasCleanRun = false,
        bool hasSingleVisualization = false,
        string? firstExportFormat = null)
    {
        PracticalTourStep firstAvailableStep = hasOpenDocument
            ? PracticalTourStep.OpenConfigurationFolder
            : PracticalTourStep.OpenDocument;
        CurrentStep = resumeStep is null or PracticalTourStep.Completed
            ? firstAvailableStep
            : resumeStep.Value;
        if (hasOpenDocument && CurrentStep == PracticalTourStep.OpenDocument)
            CurrentStep = PracticalTourStep.OpenConfigurationFolder;
        HasCleanRun = hasCleanRun && CurrentStep is PracticalTourStep.ExportReport
            or PracticalTourStep.ExportReportAnotherFormat;
        HasSingleVisualization = hasSingleVisualization
                                 && CurrentStep is PracticalTourStep.UnderstandFix
                                     or PracticalTourStep.ExportReport
                                     or PracticalTourStep.ExportReportAnotherFormat
                                     or PracticalTourStep.Completed;
        FirstExportFormat = firstExportFormat;
        IsActive = true;
    }

    /// <summary>
    /// Advances only when the host reports the action currently expected by the tour.
    /// </summary>
    /// <returns><see langword="true"/> when the state advanced; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Export steps carry a format and advance only through <see cref="ObserveReportExport"/>;
    /// a generic observation of either export step is ignored. The search-and-filters step advances
    /// only through the combination of typed text (see <see cref="ObserveSearchText"/>) and a filter
    /// change, in any order and armed from any earlier step: either half on its own only arms its side
    /// of the combination.
    /// </remarks>
    public bool Observe(PracticalTourStep completedAction)
    {
        if (!IsActive) return false;

        if (completedAction == PracticalTourStep.SearchAndFilters)
            _hasFilterUse = true;

        if (completedAction != CurrentStep
            || completedAction is PracticalTourStep.ExportReport
                or PracticalTourStep.ExportReportAnotherFormat)
            return false;

        if (completedAction == PracticalTourStep.SearchAndFilters && !_hasSearchText) return false;

        Advance();
        return true;
    }

    /// <summary>
    /// Records the current diagnostics search text and completes the search-and-filters step
    /// when the filters were already used.
    /// </summary>
    /// <param name="searchText">The current text of the diagnostics search box.</param>
    /// <returns><see langword="true"/> when the state advanced; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Clearing the box disarms the text half of the combination without advancing.</remarks>
    public bool ObserveSearchText(string searchText)
    {
        if (IsActive) _hasSearchText = !string.IsNullOrWhiteSpace(searchText);
        if (!IsActive || CurrentStep != PracticalTourStep.SearchAndFilters) return false;
        if (!_hasSearchText || !_hasFilterUse) return false;

        Advance();
        return true;
    }

    /// <summary>
    /// Completes an export step when the reported format satisfies the current expectation.
    /// </summary>
    /// <param name="format">The exporter file extension of the completed export.</param>
    /// <returns><see langword="true"/> when the state advanced; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// The first export is accepted in any format and remembered. The follow-up export advances
    /// the tour only when its format differs, so the user sees that reports come in several formats.
    /// </remarks>
    public bool ObserveReportExport(string format)
    {
        if (!IsActive) return false;

        if (CurrentStep == PracticalTourStep.ExportReport)
        {
            FirstExportFormat = format;
            Advance();
            return true;
        }

        if (CurrentStep != PracticalTourStep.ExportReportAnotherFormat
            || string.Equals(format, FirstExportFormat, StringComparison.OrdinalIgnoreCase))
            return false;

        Advance();
        return true;
    }

    /// <summary>Completes the run step and skips finding-only steps when the result is clean.</summary>
    public bool ObserveDiagnosticRun(bool hasFindings)
    {
        if (!IsActive || CurrentStep != PracticalTourStep.RunDiagnostics) return false;

        HasCleanRun = !hasFindings;
        CurrentStep = hasFindings ? PracticalTourStep.InspectFinding : PracticalTourStep.ExportReport;
        return true;
    }

    /// <summary>
    /// Completes the relevant visualization task and skips option selection when no choice is available.
    /// </summary>
    public bool ObserveVisualization(bool selectedFromMenu, int availableOptionCount)
    {
        if (!IsActive) return false;

        if (CurrentStep == PracticalTourStep.ShowElement)
        {
            HasSingleVisualization = availableOptionCount < 2;
            CurrentStep = HasSingleVisualization
                ? PracticalTourStep.UnderstandFix
                : PracticalTourStep.SelectVisualization;
            return true;
        }

        if (CurrentStep != PracticalTourStep.SelectVisualization || !selectedFromMenu) return false;

        Advance();
        return true;
    }

    /// <summary>Stops the current session while preserving no live application state.</summary>
    public void Stop()
    {
        IsActive = false;
        CurrentStep = PracticalTourStep.Completed;
    }

    private void Advance()
    {
        int currentIndex = Array.IndexOf(Steps, CurrentStep);
        CurrentStep = Steps[Math.Min(currentIndex + 1, Steps.Length - 1)];
        if (CurrentStep == PracticalTourStep.Completed) IsActive = false;
    }
}
