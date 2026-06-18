using System.Globalization;
using System.Windows.Data;

namespace Revit.Linter.DiagnosticReportPresenter.Infrasructure.Converters;

public class DebugConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        System.Diagnostics.Debug.WriteLine($"=== DEBUG CONVERTER ===");
        System.Diagnostics.Debug.WriteLine($"Value: {value}");
        System.Diagnostics.Debug.WriteLine($"Value Type: {value?.GetType()}");
        System.Diagnostics.Debug.WriteLine($"Parameter: {parameter}");

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
