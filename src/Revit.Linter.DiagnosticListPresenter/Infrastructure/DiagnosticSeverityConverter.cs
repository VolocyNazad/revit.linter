using Revit.Linter.Localization;
using System.Globalization;
using System.Windows.Data;

namespace Revit.Linter.DiagnosticListPresenter.Infrastructure;

/// <summary>
/// Converts diagnostic severity values to their localized display text for WPF bindings.
/// </summary>
public sealed class DiagnosticSeverityConverter : IValueConverter
{
    /// <summary>
    /// Returns localized text for a diagnostic severity and leaves unsupported values unchanged.
    /// </summary>
    /// <param name="value">The source binding value.</param>
    /// <param name="targetType">The binding target type.</param>
    /// <param name="parameter">The optional converter parameter.</param>
    /// <param name="culture">The binding culture.</param>
    /// <returns>The localized severity text, or <paramref name="value"/> when it is not a diagnostic severity.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is DiagnosticSeverity severity
            ? DiagnosticSeverityLocalizations.GetString(severity.ToString())
            : value;

    /// <summary>
    /// Returns the supplied value unchanged because reverse localization is not performed.
    /// </summary>
    /// <param name="value">The target binding value.</param>
    /// <param name="targetType">The binding source type.</param>
    /// <param name="parameter">The optional converter parameter.</param>
    /// <param name="culture">The binding culture.</param>
    /// <returns><paramref name="value"/> unchanged.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value;
}
