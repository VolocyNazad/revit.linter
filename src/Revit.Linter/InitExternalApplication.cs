using Autodesk.Revit.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Revit.Async;
using Revit.Context.Abstractions.Services;
using Revit.Linter.DiagnosticListPresenter.Views;
using Revit.Linter.DiagnosticReportPresenter.Views;
using Revit.Linter.FixReportPresenter.Views;
using Revit.Linter.Infrastructure.ExternalApplications;
using Revit.Linter.Infrastructure.Services;
using Revit.Linter.Infrastructure.Utils;
using Revit.TransactionMemoryCache.Abstractions.Services;
using System.Reflection;

namespace Revit.Linter;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal sealed class InitExternalApplication : ExternalApplication
{
    public override void OnStartup()
    {
        RevitTask.Initialize(Application);

        AssemblyLoadService.LoadAssemblies();
        InitializeRevitContext();
        InitializeRevitTransactionCache();
        RegisterDiagnosticReportDockablePane();
        RegisterFixReportDockablePane();
        RegisterDiagnosticListDockablePane();

        string tabName = "Volocy";
        try
        {
            Application.CreateRibbonTab(tabName);
        }
        catch { /* Вкладка уже существует - игнорируем ошибку */ }

        RibbonPanel panel = Application.CreateRibbonPanel(tabName, "Diagnostic");

        AddShowHideErrorListCommand(panel);
        AddShowHideFixListCommand(panel);
        AddShowHideDiagnosticListCommand(panel);
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
            "Show/Hide errors",
            assemblyPath,
            typeof(ShowHideErrorListCommand).FullName
        );
        //buttonData.LargeImage = LoadImage("Resources.icons.pane-icon-32.png");
        //buttonData.Image = LoadImage("Resources.icons.pane-icon-16.png");
        //buttonData.ToolTip = "";
        //buttonData.LongDescription = ""; // todo add icons

        panel.AddItem(buttonData);
    }

    private void AddShowHideFixListCommand(RibbonPanel panel)
    {
        string assemblyPath = Assembly.GetExecutingAssembly().Location;
        PushButtonData buttonData = new(
            "ShowHideFixListButton",
            "Show/Hide fixes",
            assemblyPath,
            typeof(ShowHideFixListCommand).FullName
        );
        //buttonData.LargeImage = LoadImage("Resources.icons.pane-icon-32.png");
        //buttonData.Image = LoadImage("Resources.icons.pane-icon-16.png");
        //buttonData.ToolTip = "";
        //buttonData.LongDescription = ""; // todo add icons

        panel.AddItem(buttonData);
    }

    private void AddShowHideDiagnosticListCommand(RibbonPanel panel)
    {
        string assemblyPath = Assembly.GetExecutingAssembly().Location;
        PushButtonData buttonData = new(
            "ShowHideDiagnosticListButton",
            "Show/Hide diagnostics",
            assemblyPath,
            typeof(ShowHideDiagnosticListCommand).FullName
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
        var view = Program.Provider.GetRequiredService<DiagnosticReportView>();
        var paneProvider = new DiagnosticReportDockablePaneProvider(view);
        var localizer = Program.Provider.GetRequiredService<IStringLocalizer<GlobalLocalizations>>();
        Application.RegisterDockablePane(DiagnosticReportPaneUtils.PaneId, localizer["diagnosticReport_dockablePane_title"], paneProvider);
    }

    private void RegisterFixReportDockablePane()
    {
        var view = Program.Provider.GetRequiredService<FixReportView>();
        var paneProvider = new FixReportDockablePaneProvider(view);
        var localizer = Program.Provider.GetRequiredService<IStringLocalizer<GlobalLocalizations>>();
        Application.RegisterDockablePane(FixReportPaneUtils.PaneId, localizer["fixReport_dockablePane_title"], paneProvider);
    }

    private void RegisterDiagnosticListDockablePane()
    {
        var view = Program.Provider.GetRequiredService<DiagnosticListView>();
        var paneProvider = new DiagnosticListDockablePaneProvider(view);
        var localizer = Program.Provider.GetRequiredService<IStringLocalizer<GlobalLocalizations>>();
        Application.RegisterDockablePane(DiagnosticListPaneUtils.PaneId, localizer["diagnosticList_dockablePane_title"], paneProvider);
    }
}