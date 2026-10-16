using System.Windows;
using System.Windows.Controls;

using Microsoft.Xaml.Behaviors;

namespace Revit.Linter.Behaviors;

/// <summary>
/// Opens the context menu of a button when its click offers a choice instead of a single action.
/// </summary>
/// <remarks>
/// The bound command runs on every click and handles the single-action case itself, staying a no-op
/// when several actions exist; this behavior only opens the menu in that case. It keeps menu-opening
/// view mechanics out of code-behind without teaching view models about menus.
/// </remarks>
public sealed class OpenContextMenuOnClickBehavior : Behavior<Button>
{
    /// <summary>
    /// Identifies the <see cref="OpenOnClick"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty OpenOnClickProperty = DependencyProperty.Register(
        nameof(OpenOnClick),
        typeof(bool),
        typeof(OpenContextMenuOnClickBehavior),
        new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether a click opens the context menu of the button.
    /// </summary>
    public bool OpenOnClick
    {
        get => (bool)GetValue(OpenOnClickProperty);
        set => SetValue(OpenOnClickProperty, value);
    }

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
