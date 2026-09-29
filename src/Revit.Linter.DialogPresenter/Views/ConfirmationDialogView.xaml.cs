using System.Windows;

namespace Revit.Linter.DialogPresenter.Views;

public sealed partial class ConfirmationDialogView
{
    /// <summary>
    /// Initializes a new instance of the confirmation dialog view.
    /// </summary>
    public ConfirmationDialogView() => InitializeComponent();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void ConfirmButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
