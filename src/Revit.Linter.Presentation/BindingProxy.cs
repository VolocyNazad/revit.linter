using System.Windows;

using VolocyNazad.Wpf.DependencyProperties;

namespace Revit.Linter.Presentation;

/// <summary>
/// Carries a data context into WPF objects, such as data-grid columns, that are outside the visual tree.
/// </summary>
[GenerateDependencyProperty<object>(
    "DataContext",
    PropertyXmlDocumentation = "Gets or sets the object exposed to bindings through the proxy.")]
public sealed partial class BindingProxy : Freezable
{
    /// <summary>
    /// Creates a new proxy instance for the WPF freezable infrastructure.
    /// </summary>
    /// <returns>A new binding proxy.</returns>
    protected override Freezable CreateInstanceCore() => new BindingProxy();
}
