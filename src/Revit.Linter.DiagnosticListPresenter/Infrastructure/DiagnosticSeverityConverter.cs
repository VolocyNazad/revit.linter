using Revit.Linter.Localization;
using System.Globalization;
using System.Windows.Data;

namespace Revit.Linter.DiagnosticListPresenter.Infrastructure;

public sealed class DiagnosticSeverityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is DiagnosticSeverity severity
            ? DiagnosticSeverityLocalizations.GetString(severity.ToString())
            : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value;
}
