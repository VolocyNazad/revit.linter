using System.Windows;

namespace Revit.Linter.Presentation;

/// <summary>
/// Carries a data context into WPF objects, such as data-grid columns, that are outside the visual tree.
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
    /// Gets or sets the object exposed to bindings through the proxy.
    /// </summary>
    public object DataContext
    {
        get => GetValue(DataContextProperty);
        set => SetValue(DataContextProperty, value);
    }

    /// <summary>
    /// Creates a new proxy instance for the WPF freezable infrastructure.
    /// </summary>
    /// <returns>A new binding proxy.</returns>
    protected override Freezable CreateInstanceCore() => new BindingProxy();
}
