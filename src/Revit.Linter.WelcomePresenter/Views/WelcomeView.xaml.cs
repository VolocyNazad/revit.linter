using System.Windows;

namespace Revit.Linter.WelcomePresenter.Views;

public sealed partial class WelcomeView
{
    /// <summary>
    /// Initializes a new instance of the welcome wizard window.
    /// </summary>
    public WelcomeView() => InitializeComponent();

    private void FinishButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
