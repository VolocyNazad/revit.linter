namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Describes an observed fact from the user-facing diagnostic workflow.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "CodeQuality",
    "S2094",
    Justification = "The empty base record defines a closed, strongly typed stream of optional UI facts.")]
public abstract record UserInterfaceActivity;

/// <summary>Indicates that a Revit document became available to the user.</summary>
public sealed record DocumentOpenedActivity : UserInterfaceActivity;

/// <summary>Indicates that the user opened the folder containing custom diagnostic configurations.</summary>
public sealed record ConfigurationFolderOpenedActivity : UserInterfaceActivity;

/// <summary>Indicates that the user changed whether a diagnostic is active.</summary>
/// <param name="DiagnosticCode">The stable code of the changed diagnostic.</param>
/// <param name="IsActive">The new enabled state.</param>
public sealed record DiagnosticSelectionChangedActivity(
    string DiagnosticCode,
    bool IsActive) : UserInterfaceActivity;

/// <summary>Describes a completed diagnostic run and the findings it produced.</summary>
/// <param name="DocumentTitle">The title of the document that was checked.</param>
/// <param name="Result">The aggregate outcome reported by the diagnostic service.</param>
/// <param name="FindingCount">The findings currently presented for the checked document.</param>
public sealed record DiagnosticRunCompletedActivity(
    string DocumentTitle,
    DiagnosticServiceResult Result,
    int FindingCount) : UserInterfaceActivity;

/// <summary>Indicates that the user selected a finding in the report.</summary>
/// <param name="DiagnosticCode">The stable code of the finding's diagnostic.</param>
public sealed record FindingSelectedActivity(string DiagnosticCode) : UserInterfaceActivity;

/// <summary>Indicates that an element visualization was applied successfully.</summary>
/// <param name="DiagnosticCode">The stable code of the visualized finding's diagnostic.</param>
/// <param name="SelectedFromMenu">
/// <see langword="true"/> when the user selected a named visualization from the row menu;
/// otherwise, <see langword="false"/>.
/// </param>
/// <param name="AvailableOptionCount">The number of visualization options offered for the finding.</param>
public sealed record VisualizationAppliedActivity(
    string DiagnosticCode,
    bool SelectedFromMenu = false,
    int AvailableOptionCount = 0) : UserInterfaceActivity;

/// <summary>Indicates that a fix was applied successfully.</summary>
/// <param name="DiagnosticCode">The stable code of the fixed finding's diagnostic.</param>
public sealed record FixAppliedActivity(string DiagnosticCode) : UserInterfaceActivity;

/// <summary>Indicates that the user stepped to a neighbouring finding with the report arrows.</summary>
/// <param name="DiagnosticCode">The stable code of the newly shown finding's diagnostic.</param>
public sealed record FindingNavigationUsedActivity(string DiagnosticCode) : UserInterfaceActivity;

/// <summary>Indicates that a diagnostic report was exported successfully.</summary>
public sealed record DiagnosticReportExportedActivity : UserInterfaceActivity;

/// <summary>Indicates that the user narrowed the diagnostics list with the search box or filters.</summary>
public sealed record DiagnosticListFilteredActivity : UserInterfaceActivity;

/// <summary>Indicates that the fix list pane became visible.</summary>
public sealed record FixListPaneShownActivity : UserInterfaceActivity;

/// <summary>Indicates that the user selected an applied fix in the fix list.</summary>
/// <param name="DiagnosticCode">The stable code of the selected fix's diagnostic.</param>
public sealed record FixSelectedActivity(string DiagnosticCode) : UserInterfaceActivity;
