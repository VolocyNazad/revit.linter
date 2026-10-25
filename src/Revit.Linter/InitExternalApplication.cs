using Autodesk.Revit.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Async;
using Revit.Context.Abstractions.Services;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.ElementChangesMonitor.Abstractions.Services;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;
using Revit.Linter.Infrastructure.ExternalApplications;
using Revit.Linter.Infrastructure.Services;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
using Revit.TransactionMemoryCache.Abstractions.Services;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
#if BEFORE2024
using Revit.Sugar;
#endif

namespace Revit.Linter;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
internal sealed partial class InitExternalApplication : ExternalApplication
{
    private static readonly string AssemblyPath = Assembly.GetExecutingAssembly().Location;
    private static readonly string AssemblyDirectory = Path.GetDirectoryName(AssemblyPath)
        ?? throw new InvalidOperationException("The executing assembly path has no directory.");
    private static readonly string IconPath = Path.Combine(AssemblyDirectory, "Resources", "None Icon.tiff");
    private static IStringLocalizer<GlobalLocalizations> Localizer =>
        GetService<IStringLocalizer<GlobalLocalizations>>();
    private static ILogger<InitExternalApplication> Logger => GetService<ILogger<InitExternalApplication>>();

    // Every ribbon button shows the same placeholder icon, so it is read once per size.
    private static BitmapImage Icon => field ??= new(new Uri(IconPath));
    private static BitmapImage SmallIcon => field ??= LoadImage(IconPath, 16);
    private static PushButton? _tutorialSampleButton;
    private DiagnosticCatalogNotifier? _diagnosticCatalogNotifier;
    private ValueStoreNotifier? _valueStoreNotifier;

    public override void OnStartup()
    {
        try
        {
            StartApplication();
        }
        catch (Exception exception)
        {
            Serilog.Log.Fatal(exception, "Revit.Linter failed to start");
            Result = Autodesk.Revit.UI.Result.Failed;
            Program.Shutdown();
        }
    }

    private void StartApplication()
    {
        RevitTask.Initialize(Application);
        GetService<RevitIdlingScheduler>().Initialize(Application);

        AssemblyLoadService.LoadAssemblies();

        InitializeRevitContext();
        InitializeRevitTransactionCache();
        _diagnosticCatalogNotifier = GetService<DiagnosticCatalogNotifier>();
        _valueStoreNotifier = GetService<ValueStoreNotifier>();
        RegisterDockablePanes();
        HidePracticalTourPaneWithoutSession();

        string tabName = Localizer["ribbonTab_name"];
        try
        {
            Application.CreateRibbonTab(tabName);
        }
        catch (Exception exception)
        {
            // Revit throws when the tab already exists, which is expected after an add-in reload.
            Logger.LogDebug(exception, "The Revit Linter ribbon tab already exists");
        }

        RibbonPanel panel = Application.CreateRibbonPanel(tabName, Localizer["ribbonPanel_diagnostics_name"]);

        AddRibbonButtons(panel);
        AddTestToolingButtons(tabName);

        GetService<IElementChangesMonitor>().Run();

        var app = Application.ControlledApplication;
        app.DocumentCreated += App_DocumentCreated;
        app.DocumentOpened += App_DocumentOpened;
        app.DocumentClosing += App_DocumentClosing;
        app.DocumentClosed += App_DocumentClosed;
        Application.ViewActivated += App_ViewActivated;

        GetService<ITutorialSampleCopyService>().CleanupAbandonedCopies(GetService<IConfigurationPaths>().RevitVersion);

#if !BEFORE2024
        InitializeThemeHandling();
#endif

        string productVersion = typeof(InitExternalApplication).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(InitExternalApplication).Assembly.GetName().Version?.ToString()
            ?? "unknown";
        Logger.LogInformation(
            "Revit.Linter {ProductVersion} started (Revit {RevitVersion})",
            productVersion,
            Application.ControlledApplication.VersionNumber);

        ScheduleWelcomeWizard();
        ScheduleConfigurationMigrationNotice();
    }


    public override void OnShutdown()
    {
        ILogger? logger = null;
        try
        {
            logger = Logger;
            logger.LogInformation("Revit.Linter shutting down");

            GetService<IElementChangesMonitor>().Stop();
            _diagnosticCatalogNotifier?.Dispose();
            _diagnosticCatalogNotifier = null;
            _valueStoreNotifier?.Dispose();
            _valueStoreNotifier = null;

            var app = Application.ControlledApplication;
            app.DocumentCreated -= App_DocumentCreated;
            app.DocumentOpened -= App_DocumentOpened;
            app.DocumentClosing -= App_DocumentClosing;
            app.DocumentClosed -= App_DocumentClosed;
            Application.ViewActivated -= App_ViewActivated;

#if !BEFORE2024
            Application.ThemeChanged -= Application_ThemeChanged;
#endif

            GetService<RevitIdlingScheduler>().Dispose();
            RevitTask.Shutdown();
        }
        catch (Exception exception)
        {
            // The host may already be disposed after a failed startup; the static Serilog logger
            // still records the failure.
            if (logger is null)
                Serilog.Log.Error(exception, "Revit.Linter failed to shut down cleanly");
            else
                logger.LogError(exception, "Revit.Linter failed to shut down cleanly");
        }
        finally
        {
            Program.Shutdown();
        }
    }


    private static BitmapImage LoadImage(string path, int pixelSize)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = pixelSize;
        image.DecodePixelHeight = pixelSize;
        image.UriSource = new Uri(path);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private void InitializeRevitContext()
        => GetService<IRevitContextInitializer>().Initialize(Application);

    private static void InitializeRevitTransactionCache()
    {
        GetService<IRevitTransactionMemoryCacheInitializer>().Initialize();
        DocumentElementCollectorCache.Initialize(GetService<IDocumentQueryService>());
    }


    private static T GetService<T>() where T : notnull => Program.Provider.GetRequiredService<T>();
}
