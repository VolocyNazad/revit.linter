using Revit.Linter.Localization;
using Revit.Linter.Presentation.ViewModels;

namespace Revit.Linter.DiagnosticReportPresenter.ViewModels;

internal sealed partial class DiagnosticSeverityFilterViewModel(
    bool isActive, DiagnosticSeverity value, int count)
    : FilterViewModel<DiagnosticReportItemViewModel, DiagnosticSeverity>(
        isActive,
        value,
        count,
        item => item.Severity == value,
        () => DiagnosticSeverityLocalizations.GetString(value.ToString()));
