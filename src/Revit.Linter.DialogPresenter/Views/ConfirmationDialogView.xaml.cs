using System.Windows;

namespace Revit.Linter.DialogPresenter.Views;

public sealed partial class ConfirmationDialogView
{
    public ConfirmationDialogView() => InitializeComponent();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void ConfirmButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
