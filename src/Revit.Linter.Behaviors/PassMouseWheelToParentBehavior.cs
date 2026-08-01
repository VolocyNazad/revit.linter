using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.Xaml.Behaviors;

namespace Revit.Linter.Behaviors;

public sealed class PassMouseWheelToParentBehavior : Behavior<FlowDocumentScrollViewer>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
    }

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
