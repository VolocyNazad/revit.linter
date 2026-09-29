using System.Windows;

namespace Revit.Linter.DiagnosticReportPresenter.Infrastructure;

/// <summary>
/// Exposes a data context to bindings that cannot inherit it through the WPF visual tree.
/// </summary>
public sealed class BindingProxy : Freezable
{
    /// <summary>
    /// Identifies the <see cref="DataContext"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty DataContextProperty
        = DependencyProperty.Register(
            nameof(FrameworkElement.DataContext),
            typeof(object),
            typeof(BindingProxy));

    /// <summary>
    /// Gets or sets the data context exposed by the proxy.
    /// </summary>
    public object DataContext
    {
        get => GetValue(DataContextProperty);
        set => SetValue(DataContextProperty, value);
    }

    /// <inheritdoc />
    protected override Freezable CreateInstanceCore() => new BindingProxy();
}
