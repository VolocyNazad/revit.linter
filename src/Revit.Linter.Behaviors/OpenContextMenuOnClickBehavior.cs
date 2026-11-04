using System.Windows;
using System.Windows.Controls;

using Microsoft.Xaml.Behaviors;

using VolocyNazad.Wpf.DependencyProperties;

namespace Revit.Linter.Behaviors;

/// <summary>
/// Opens the context menu of a button when its click offers a choice instead of a single action.
/// </summary>
/// <remarks>
/// The bound command runs on every click and handles the single-action case itself, staying a no-op
/// when several actions exist; this behavior only opens the menu in that case. It keeps menu-opening
/// view mechanics out of code-behind without teaching view models about menus.
/// </remarks>
[GenerateDependencyProperty<bool>(
    "OpenOnClick",
    PropertyXmlDocumentation = "Gets or sets whether a click opens the context menu of the button.")]
public sealed partial class OpenContextMenuOnClickBehavior : Behavior<Button>
{
    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Click += OnClick;
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        AssociatedObject.Click -= OnClick;
        base.OnDetaching();
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        if (!OpenOnClick) return;
        if (AssociatedObject.ContextMenu is null) return;

        AssociatedObject.ContextMenu.PlacementTarget = AssociatedObject;
        AssociatedObject.ContextMenu.IsOpen = true;
    }
}
