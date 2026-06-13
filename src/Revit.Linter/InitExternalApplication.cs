using Autodesk.Revit.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Infrastructure.ExternalApplications;
using Revit.Linter.Infrastructure.Services;
using Revit.Linter.Infrastructure.Utils;
using Revit.Linter.Views;
using Revit.TransactionMemoryCache.Abstractions.Services;
using System.Reflection;
using System.Windows.Controls;

namespace Revit.Linter;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal sealed class InitExternalApplication : ExternalApplication
{
    public override void OnStartup()
    {
        AssemblyLoadService.LoadAssemblies();
        InitializeRevitContext();
        InitializeRevitTransactionCache();
        RegisterDiagnosticReportDockablePane();

        string tabName = "Volocy";
        try
        {
            Application.CreateRibbonTab(tabName);
        }
        catch { /* Вкладка уже существует - игнорируем ошибку */ }

        RibbonPanel panel = Application.CreateRibbonPanel(tabName, "Diagnostic");

        AddShowHideErrorListCommand(panel);
        AddOpenConfigurationFolderCommand(panel);
    }

    private void AddOpenConfigurationFolderCommand(RibbonPanel panel)
    {
        string assemblyPath = Assembly.GetExecutingAssembly().Location;
        PushButtonData buttonData = new(
            "OpenConfigurationFolderButton",
            "Open configuration folder",
            assemblyPath,
            typeof(OpenConfigurationFolderCommand).FullName
        );
        //buttonData.LargeImage = LoadImage("Resources.icons.pane-icon-32.png");
        //buttonData.Image = LoadImage("Resources.icons.pane-icon-16.png");
        //buttonData.ToolTip = "";
        //buttonData.LongDescription = ""; // todo add icons

        panel.AddItem(buttonData);
    }
    private void AddShowHideErrorListCommand(RibbonPanel panel)
    {
        string assemblyPath = Assembly.GetExecutingAssembly().Location;
        PushButtonData buttonData = new(
            "ShowHideErrorListButton",
            "Show/Hide diagnostic",
            assemblyPath,
            typeof(ShowHideErrorListCommand).FullName
        );
        //buttonData.LargeImage = LoadImage("Resources.icons.pane-icon-32.png");
        //buttonData.Image = LoadImage("Resources.icons.pane-icon-16.png");
        //buttonData.ToolTip = "";
        //buttonData.LongDescription = ""; // todo add icons

        panel.AddItem(buttonData);
    }
    private void InitializeRevitContext()
        => Program.Provider.GetRequiredService<IRevitContextInitializer>().Initialize(Application);
    private void InitializeRevitTransactionCache()
        => Program.Provider.GetRequiredService<IRevitTransactionMemoryCacheInitializer>().Initialize();
    private void RegisterDiagnosticReportDockablePane()
    {
        var view = new MainView();
        var paneProvider = new DiagnosticReportDockablePaneProvider(view);
        var localizer = Program.Provider.GetRequiredService<IStringLocalizer<GlobalLocalizations>>();
        Application.RegisterDockablePane(DiagnosticPaneUtils.PaneId, localizer["diagnosticReport_dockablePane_title"], paneProvider);
    }
}

public class DiagnosticReportDockablePaneProvider(UserControl uiControl) : IDockablePaneProvider
{
    public void SetupDockablePane(DockablePaneProviderData data)
    {
        data.FrameworkElement = uiControl;
        data.InitialState = new()
        {
            DockPosition = DockPosition.Bottom,
        };
    }
}