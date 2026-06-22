using System.Windows;

namespace Revit.Linter.DiagnosticListPresenter.Infrasructure;

public sealed class BindingProxy : Freezable
{
    public static readonly DependencyProperty DataContextProperty
        = DependencyProperty.Register(
            nameof(FrameworkElement.DataContext),
            typeof(object),
            typeof(BindingProxy));

    public object DataContext
    {
        get => GetValue(DataContextProperty);
        set => SetValue(DataContextProperty, value);
    }

    protected override Freezable CreateInstanceCore() => new BindingProxy();
}
