using System.Windows;

namespace Revit.Linter.StatusBar.Views;

public sealed partial class ThemeView
{
    public ThemeView()
    {
        InitializeComponent();
        InitializeWindow();

        CreateProgressBar("Autodesk Revit 2019", "default");
        CreateProgressBar("Autodesk Revit 2020", "default");
        CreateProgressBar("Autodesk Revit 2021", "default");
        CreateProgressBar("Autodesk Revit 2022", "default");
        CreateProgressBar("Autodesk Revit 2023", "default");
        CreateProgressBar("Autodesk Revit 2024", "light");
        CreateProgressBar("Autodesk Revit 2024", "dark");

        this.KeyDown += (s, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
            }
            if (e.Key == System.Windows.Input.Key.R)
            {
                foreach (var progressBar in Root.Children.OfType<ProgressBarStackPanel>())
                {
                    Run(progressBar);
                }
            }
        };

    }

    private void CreateProgressBar(string title, string? theme = null)
    {
        var progressBar = new ProgressBarStackPanel(true);
        progressBar.SetCurrentOperation(title);
        progressBar.AddResourceThemes(theme);
        Root.Children.Add(progressBar);
        Run(progressBar);
    }

    private void Run(ProgressBarStackPanel progressBar)
    {
        Task.Run(async () =>
        {
            progressBar.Data.CurrentValue = 0;
            await Task.Delay(1000);
            progressBar.Data.CurrentValue = 0;
            for (int i = (int)progressBar.Data.MinimumValue; i <= (int)progressBar.Data.MaximumValue; i++)
            {
                progressBar.Data.CurrentValue = i;
                await Task.Delay(10);
            }
        });
    }

    private void InitializeWindow()
    {
        Width = 500;
        SizeToContent = SizeToContent.Height;
        WindowStyle = WindowStyle.None;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        new System.Windows.Interop.WindowInteropHelper(this) { Owner = Autodesk.Windows.ComponentManager.ApplicationWindow };
    }
}