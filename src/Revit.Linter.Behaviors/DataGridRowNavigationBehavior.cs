using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Xaml.Behaviors;

namespace Revit.Linter.Behaviors;

/// <summary>
/// Lets a read-only data grid be walked row by row from the keyboard and keeps its selected row in view.
/// </summary>
/// <remarks>
/// Up and Down select the neighbouring row and Enter executes <see cref="ActivateCommand"/>. The keys are
/// taken before the grid and its cells see them, because a selectable text box inside a cell otherwise
/// consumes the arrows; key presses with a modifier are left alone. Whenever the selection changes, by
/// these keys or from a view model, the selected row is scrolled into view. Do not attach the behavior to
/// a grid whose cells are edited with the arrow or Enter keys.
/// </remarks>
public sealed class DataGridRowNavigationBehavior : Behavior<DataGrid>
{
    /// <summary>
    /// Identifies the <see cref="ActivateCommand"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ActivateCommandProperty = DependencyProperty.Register(
        nameof(ActivateCommand),
        typeof(ICommand),
        typeof(DataGridRowNavigationBehavior),
        new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command executed when Enter is pressed in the grid.
    /// </summary>
    public ICommand? ActivateCommand
    {
        get => (ICommand?)GetValue(ActivateCommandProperty);
        set => SetValue(ActivateCommandProperty, value);
    }

    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
        AssociatedObject.SelectionChanged += OnSelectionChanged;
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
        AssociatedObject.SelectionChanged -= OnSelectionChanged;
        base.OnDetaching();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.None) return;

        switch (e.Key)
        {
            case Key.Up:
                MoveSelection(-1);
                e.Handled = true;
                break;
            case Key.Down:
                MoveSelection(1);
                e.Handled = true;
                break;
            case Key.Enter when ActivateCommand?.CanExecute(null) == true:
                ActivateCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void MoveSelection(int step)
    {
        DataGrid grid = AssociatedObject;
        int count = grid.Items.Count;
        if (count == 0) return;

        int index = grid.SelectedIndex < 0
            ? (step > 0 ? 0 : count - 1)
            : Math.Max(0, Math.Min(count - 1, grid.SelectedIndex + step));
        grid.SelectedIndex = index;

        // Keep the keyboard in the grid: the cell that had the focus may belong to a row that has just
        // scrolled out of view.
        grid.Focus();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Selectors inside the cells, such as a combo box, raise the same routed event.
        if (!ReferenceEquals(e.OriginalSource, AssociatedObject)) return;

        if (AssociatedObject.SelectedItem is { } item)
            AssociatedObject.ScrollIntoView(item);
    }
}
