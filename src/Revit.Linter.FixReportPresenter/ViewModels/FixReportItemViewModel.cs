using Revit.Linter.Behaviors;
using Revit.Linter.Presentation.ViewModels;

namespace Revit.Linter.FixReportPresenter.ViewModels;

/// <summary>
/// A row of the fix report: the shared report identity and message plus the fix-specific columns.
/// </summary>
internal sealed partial class FixReportItemViewModel : ReportRowViewModel, IHasDiagnosticCode
{
}
