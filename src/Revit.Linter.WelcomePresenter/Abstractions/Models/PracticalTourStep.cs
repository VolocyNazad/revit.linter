namespace Revit.Linter.WelcomePresenter.Abstractions.Models;

/// <summary>Names the user action currently explained by the optional practical tour.</summary>
public enum PracticalTourStep
{
    /// <summary>The tour is waiting for the user to open a project or family.</summary>
    OpenDocument = 0,

    /// <summary>The tour shows where custom diagnostic configuration files are stored.</summary>
    OpenConfigurationFolder = 8,

    /// <summary>The tour explains how to narrow the diagnostics list with search and filters.</summary>
    SearchAndFilters = 11,

    /// <summary>The tour points out how diagnostics are selected.</summary>
    SelectDiagnostic = 1,

    /// <summary>The tour points out how the selected diagnostics are run.</summary>
    RunDiagnostics = 2,

    /// <summary>The tour explains how to inspect a finding.</summary>
    InspectFinding = 3,

    /// <summary>The tour explains how to show a finding in Revit.</summary>
    ShowElement = 4,

    /// <summary>The tour explains how to choose another visualization from the row menu.</summary>
    SelectVisualization = 9,

    /// <summary>The tour explains how to step to neighbouring findings with the report arrows.</summary>
    NavigateFindings = 10,

    /// <summary>The tour explains that fixes remain explicit user actions.</summary>
    UnderstandFix = 5,

    /// <summary>The tour points out the fix list pane where applied fixes appear.</summary>
    FixList = 12,

    /// <summary>The tour points out report export.</summary>
    ExportReport = 6,

    /// <summary>The tour asks for the same report in another export format.</summary>
    ExportReportAnotherFormat = 13,

    /// <summary>The practical tour has reached its final step.</summary>
    Completed = 7,
}
