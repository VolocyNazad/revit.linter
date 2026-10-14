using Revit.Linter.Localization;
using Revit.Linter.Presentation.ViewModels;

namespace Revit.Linter.DiagnosticListPresenter.ViewModels;

internal sealed partial class DiagnosticTargetTypeFilterViewModel(TargetType value, int count)
    : FilterViewModel<DiagnosticItemViewModel, TargetType>(
        isActive: true,
        value,
        count,
        item => item.TargetType == value,
        () => DiagnosticTargetTypeLocalizations.GetString(value.ToString()));
