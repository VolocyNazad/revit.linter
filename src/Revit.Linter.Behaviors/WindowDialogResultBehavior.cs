using System.Windows;

using Microsoft.Xaml.Behaviors;

namespace Revit.Linter.Behaviors;

/// <summary>
/// Applies a view-model requested dialog result to a modal window.
/// </summary>
/// <remarks>
/// Setting <see cref="DialogResult"/> to a value assigns <see cref="Window.DialogResult"/>, which also
/// closes a window shown with <c>ShowDialog</c>; a null value does nothing. Attach only to modal
/// dialogs: assigning the result of a modeless window throws. The view model resets the bound
/// property before and after every show, so a stale result never closes the next show on attach.
/// </remarks>
public sealed class WindowDialogResultBehavior : Behavior<Window>
{
    /// <summary>
    /// Identifies the <see cref="DialogResult"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty DialogResultProperty = DependencyProperty.Register(
        nameof(DialogResult),
        typeof(bool?),
        typeof(WindowDialogResultBehavior),
        new PropertyMetadata(null, OnDialogResultChanged));

    /// <summary>
    /// Gets or sets the dialog result requested by the view model; null requests nothing.
    /// </summary>
    public bool? DialogResult
    {
        get => (bool?)GetValue(DialogResultProperty);
        set => SetValue(DialogResultProperty, value);
    }

    private static void OnDialogResultChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
    {
        if (source is not WindowDialogResultBehavior behavior) return;
        if (behavior.AssociatedObject is null) return;
        if (args.NewValue is not bool result) return;

        behavior.AssociatedObject.DialogResult = result;
    }
}
