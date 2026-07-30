using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Revit.Async;
using Revit.Context.Abstractions.Services;
using Revit.Linter.DiagnosticListPresenter.Views;
using Revit.Linter.DiagnosticReportPresenter.Views;
using Revit.Linter.ElementChangesMonitor.Abstractions.Services;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;
using Revit.Linter.FixReportPresenter.Views;
using Revit.Linter.Infrastructure.ExternalApplications;
using Revit.Linter.Infrastructure.Services;
using Revit.Linter.Infrastructure.Utils;
using Revit.Linter.ProjectParameterManaging.Abstractions.Services;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using Revit.TransactionMemoryCache.Abstractions.Services;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;
#if BEFORE2024
using Toolkit.Revit.Extensions;
#endif

namespace Revit.Linter;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal sealed class InitExternalApplication : ExternalApplication
{
    private static readonly string AssemblyPath = Assembly.GetExecutingAssembly().Location;
    private static readonly string AssemblyDirectory = Path.GetDirectoryName(AssemblyPath);

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

        var elementChangesMonitor = Program.Provider.GetRequiredService<IElementChangesMonitor>();
        elementChangesMonitor.Run();

        var app = Application.ControlledApplication;
        app.DocumentCreated += App_DocumentCreated;
        app.DocumentOpened += App_DocumentOpened;

#if !BEFORE2024
        InitializeThemeHandling();
#endif
    }

    public override void OnShutdown()
    {
        var app = Application.ControlledApplication;
        app.DocumentCreated -= App_DocumentCreated;
        app.DocumentOpened -= App_DocumentOpened;

#if !BEFORE2024
        Application.ThemeChanged -= Application_ThemeChanged;
#endif
    }

#if !BEFORE2024
    private void InitializeThemeHandling()
    {
        ChangePluginTheme();
        Application.ThemeChanged += Application_ThemeChanged;
    }

    private static void Application_ThemeChanged(object? sender, Autodesk.Revit.UI.Events.ThemeChangedEventArgs e)
        => ChangePluginTheme();

    private static void ChangePluginTheme()
    {
        bool isDarkTheme = UIThemeManager.CurrentTheme == UITheme.Dark;
        MediaColor backgroundColor = GetRevitFrameBackgroundColor(isDarkTheme);

        Program.Provider.GetRequiredService<IThemeService>().ChangeTheme(isDarkTheme, backgroundColor);
    }

    private static MediaColor GetRevitFrameBackgroundColor(bool isDarkTheme)
    {
        var method = typeof(UIThemeManager).GetMethod("GetCurrentFrameBackgroundColor", Type.EmptyTypes);
        object? revitColor = method?.Invoke(null, null);
        if (revitColor is null)
            return isDarkTheme ? MediaColor.FromRgb(44, 52, 64) : MediaColor.FromRgb(245, 245, 245);

        var colorType = revitColor.GetType();
        return MediaColor.FromRgb(
            Convert.ToByte(colorType.GetProperty("Red")?.GetValue(revitColor)),
            Convert.ToByte(colorType.GetProperty("Green")?.GetValue(revitColor)),
            Convert.ToByte(colorType.GetProperty("Blue")?.GetValue(revitColor)));
    }
#endif

    private static async void App_DocumentOpened(object? sender, DocumentOpenedEventArgs e) => await AddProjectParameters(e.Document);

    private static async void App_DocumentCreated(object? sender, DocumentCreatedEventArgs e) => await AddProjectParameters(e.Document);

    private static async Task AddProjectParameters(Document doc) => await AddIgnoreListParameter(doc);

    private static async Task AddIgnoreListParameter(Document doc)
    {
        var projectParameterProvider = Program.Provider.GetRequiredService<IProjectParameterProvider>();

        bool parameterChanged = false;

        await RevitTask.RunAsync(() =>
        {
            using (Transaction transaction = new(doc, "Parameter project adding"))
            {
                transaction.Start();

                parameterChanged = projectParameterProvider.Add(
                    doc, new Guid("666a739a-ae5d-48d1-b146-fc0b2d7f5a4b"),
                    doc.Settings.Categories.Cast<Category>().Where(i => i.AllowsBoundParameters).Select(i => i.BuiltInCategory).ToList(),
#if BEFORE2024
                    BuiltInParameterGroup.PG_IDENTITY_DATA,
#else
                    GroupTypeId.IdentityData,
#endif
                    true
                );

                transaction.Commit();
            }

            if (parameterChanged)
                TaskDialog.Show("Information", "Project parameters configured!"); // todo Use custom dialog
        });
    }

    private static void AddOpenConfigurationFolderCommand(RibbonPanel panel)
    {
        PushButtonData buttonData = new(
            "OpenConfigurationFolderButton",
            "Open configuration folder",
            AssemblyPath, typeof(OpenConfigurationFolderCommand).FullName)
        {
            ToolTip = "Open linter configuration folder",
            LongDescription = "Opens the folder containing configuration files that define the linter's logic.", // todo add images
            LargeImage = LoadImage(Path.Combine(AssemblyDirectory, "Resources", "None Icon.tiff")),
            Image = LoadImage(Path.Combine(AssemblyDirectory, "Resources", "None Icon.tiff")),
            ToolTipImage = LoadImage(Path.Combine(AssemblyDirectory, "Resources", "None Icon.tiff"))

        };

        panel.AddItem(buttonData);
    }

    private static void AddShowHideErrorListCommand(RibbonPanel panel)
    {
        PushButtonData buttonData = new(
            "ShowHideErrorListButton",
            "Show/Hide errors",
            AssemblyPath, typeof(ShowHideErrorListCommand).FullName)
        {
            ToolTip = "Show/Hide error pane",
            LongDescription = "Shows or hides the error list panel that displays all linter's diagnostics.", // todo add images
            LargeImage = LoadImage(Path.Combine(AssemblyDirectory, "Resources", "None Icon.tiff")),
            Image = LoadImage(Path.Combine(AssemblyDirectory, "Resources", "None Icon.tiff")),
            ToolTipImage = LoadImage(Path.Combine(AssemblyDirectory, "Resources", "None Icon.tiff"))
        };

        panel.AddItem(buttonData);
    }

    private static void AddShowHideFixListCommand(RibbonPanel panel)
    {
        PushButtonData buttonData = new(
            "ShowHideFixListButton",
            "Show/Hide fixes",
            AssemblyPath, typeof(ShowHideFixListCommand).FullName)
        {
            ToolTip = "Show/Hide fix pane",
            LongDescription = "Shows or hides the fix report panel that displays all linter's fix report history.", // todo add images
            LargeImage = LoadImage(Path.Combine(AssemblyDirectory, "Resources", "None Icon.tiff")),
            Image = LoadImage(Path.Combine(AssemblyDirectory, "Resources", "None Icon.tiff")),
            ToolTipImage = LoadImage(Path.Combine(AssemblyDirectory, "Resources", "None Icon.tiff"))
        };

        panel.AddItem(buttonData);
    }

    private static BitmapImage LoadImage(string path) => new(new Uri(path));

    private static void AddShowHideDiagnosticListCommand(RibbonPanel panel)
    {
        PushButtonData buttonData = new(
            "ShowHideDiagnosticListButton",
            "Show/Hide diagnostics",
            AssemblyPath, typeof(ShowHideDiagnosticListCommand).FullName)
        {
            ToolTip = "Show/Hide diagnostic pane",
            LongDescription = "Shows or hides the diagnostic report panel that displays all linter's report history.", // todo add images
            LargeImage = LoadImage(Path.Combine(AssemblyDirectory, "Resources", "None Icon.tiff")),
            Image = LoadImage(Path.Combine(AssemblyDirectory, "Resources", "None Icon.tiff")),
            ToolTipImage = LoadImage(Path.Combine(AssemblyDirectory, "Resources", "None Icon.tiff"))
        };

        panel.AddItem(buttonData);
    }

    private void InitializeRevitContext()
        => Program.Provider.GetRequiredService<IRevitContextInitializer>().Initialize(Application);

    private static void InitializeRevitTransactionCache()
    {
        Program.Provider.GetRequiredService<IRevitTransactionMemoryCacheInitializer>().Initialize();
        DocumentElementCollectorCache.Initialize(
            Program.Provider.GetRequiredService<IRevitTransactionMemoryCache>());
    }

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
