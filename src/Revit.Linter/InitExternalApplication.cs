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
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace Revit.Linter;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal sealed class InitExternalApplication : ExternalApplication
{
    private static string _assemblyPath = Assembly.GetExecutingAssembly().Location;
    private static string _assemblyDirectory = Path.GetDirectoryName(_assemblyPath);

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

    private static void AddOpenConfigurationFolderCommand(RibbonPanel panel)
    {
        PushButtonData buttonData = new(
            "OpenConfigurationFolderButton",
            "Open configuration folder",
            _assemblyPath, typeof(OpenConfigurationFolderCommand).FullName)
        {
            ToolTip = "",
            LongDescription = "", // todo add images, tooltip and description
            LargeImage = LoadImage(Path.Combine(_assemblyDirectory, "Resources", "None Icon.tiff")),
            Image = LoadImage(Path.Combine(_assemblyDirectory, "Resources", "None Icon.tiff")),
            ToolTipImage = LoadImage(Path.Combine(_assemblyDirectory, "Resources", "None Icon.tiff"))

        };

        panel.AddItem(buttonData);
    }

    private static void AddShowHideErrorListCommand(RibbonPanel panel)
    {
        PushButtonData buttonData = new(
            "ShowHideErrorListButton",
            "Show/Hide errors",
            _assemblyPath, typeof(ShowHideErrorListCommand).FullName)
        {
            ToolTip = "",
            LongDescription = "", // todo add images, tooltip and description
            LargeImage = LoadImage(Path.Combine(_assemblyDirectory, "Resources", "None Icon.tiff")),
            Image = LoadImage(Path.Combine(_assemblyDirectory, "Resources", "None Icon.tiff")),
            ToolTipImage = LoadImage(Path.Combine(_assemblyDirectory, "Resources", "None Icon.tiff"))
        };

        panel.AddItem(buttonData);
    }

    private static void AddShowHideFixListCommand(RibbonPanel panel)
    {
        PushButtonData buttonData = new(
            "ShowHideFixListButton",
            "Show/Hide fixes",
            _assemblyPath, typeof(ShowHideFixListCommand).FullName)
        {
            ToolTip = "",
            LongDescription = "", // todo add images, tooltip and description
            LargeImage = LoadImage(Path.Combine(_assemblyDirectory, "Resources", "None Icon.tiff")),
            Image = LoadImage(Path.Combine(_assemblyDirectory, "Resources", "None Icon.tiff")),
            ToolTipImage = LoadImage(Path.Combine(_assemblyDirectory, "Resources", "None Icon.tiff"))
        };

        panel.AddItem(buttonData);
    }

    private static BitmapImage LoadImage(string path) => new(new Uri(path));

    private static void AddShowHideDiagnosticListCommand(RibbonPanel panel)
    {
        PushButtonData buttonData = new(
            "ShowHideDiagnosticListButton",
            "Show/Hide diagnostics",
            _assemblyPath, typeof(ShowHideDiagnosticListCommand).FullName)
        {
            ToolTip = "",
            LongDescription = "", // todo add images, tooltip and description
            LargeImage = LoadImage(Path.Combine(_assemblyDirectory, "Resources", "None Icon.tiff")),
            Image = LoadImage(Path.Combine(_assemblyDirectory, "Resources", "None Icon.tiff")),
            ToolTipImage = LoadImage(Path.Combine(_assemblyDirectory, "Resources", "None Icon.tiff"))
        };

        panel.AddItem(buttonData);
    }

    private void InitializeRevitContext()
        => Program.Provider.GetRequiredService<IRevitContextInitializer>().Initialize(Application);

    private static void InitializeRevitTransactionCache()
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