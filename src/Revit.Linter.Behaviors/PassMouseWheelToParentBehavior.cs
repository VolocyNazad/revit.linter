using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.Xaml.Behaviors;

namespace Revit.Linter.Behaviors;

/// <summary>
/// Forwards mouse-wheel input from a flow-document viewer to its visual parent so an enclosing scroller can respond.
/// </summary>
public sealed class PassMouseWheelToParentBehavior : Behavior<FlowDocumentScrollViewer>
{
    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseWheel -= OnPreviewMouseWheel;
        base.OnDetaching();
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;

        var parent = VisualTreeHelper.GetParent(AssociatedObject) as UIElement;
        parent?.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = Mouse.MouseWheelEvent,
        });
    }
}
