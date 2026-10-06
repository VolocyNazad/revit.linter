using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Async;
using Revit.Context.Abstractions.Services;
using Revit.Linter.DiagnosticListPresenter.Views;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DiagnosticReportPresenter.Views;
using Revit.Linter.DialogPresenter.Abstractions;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.ElementChangesMonitor.Abstractions.Services;
using Revit.Linter.ElementDependencyDefiners.Infrastructure;
using Revit.Linter.FixReportPresenter.Views;
using Revit.Linter.Infrastructure.ExternalApplications;
using Revit.Linter.Infrastructure.Services;
using Revit.Linter.Infrastructure.Utils;
using Revit.Linter.ProjectParameterManaging.Abstractions.Services;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using Revit.Linter.WelcomePresenter.Views;
using Revit.Linter.ConfigurationPath;
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
internal sealed class InitExternalApplication : ExternalApplication
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

        string tabName = Localizer["ribbonTab_name"];
        try
        {
            Application.CreateRibbonTab(tabName);
        }
        catch { /* Tab already exists - ignore the error */ }

        RibbonPanel panel = Application.CreateRibbonPanel(tabName, Localizer["ribbonPanel_diagnostics_name"]);

        AddRibbonButtons(panel);

        GetService<IElementChangesMonitor>().Run();

        var app = Application.ControlledApplication;
        app.DocumentCreated += App_DocumentCreated;
        app.DocumentOpened += App_DocumentOpened;
        app.DocumentClosing += App_DocumentClosing;
        app.DocumentClosed += App_DocumentClosed;

        GetService<ITutorialSampleCopyService>().CleanupAbandonedCopies(ConfigurationPathUtils.RevitVersion);

#if !BEFORE2024
        InitializeThemeHandling();
#endif

        Logger.LogInformation("Revit.Linter started (Revit {Version})", Application.ControlledApplication.VersionNumber);

        ScheduleWelcomeWizard();
    }

    /// <summary>
    /// Queues the first-run welcome wizard for the first Idling event, when the Revit main window exists.
    /// </summary>
    /// <remarks>
    /// A failure is logged and never affects add-in startup; the ribbon command remains available.
    /// Debug builds show every step on each start so the wizard can be worked on without resetting the
    /// stored state; release builds show only the steps the user has not seen.
    /// </remarks>
    private static void ScheduleWelcomeWizard() =>
        _ = GetService<RevitIdlingScheduler>().RunAsync(_ =>
        {
            try
            {
                var wizard = GetService<IWelcomeWizard>();
#if DEBUG
                wizard.Show();
#else
                wizard.ShowIfNeeded();
#endif
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to show the welcome wizard on startup");
            }
        });

    public override void OnShutdown()
    {
        ILogger logger = Logger;
        logger.LogInformation("Revit.Linter shutting down");

        try
        {
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

#if !BEFORE2024
            Application.ThemeChanged -= Application_ThemeChanged;
#endif

            GetService<RevitIdlingScheduler>().Dispose();
            RevitTask.Shutdown();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Revit.Linter failed to shut down cleanly");
        }
        finally
        {
            Program.Shutdown();
        }
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
        GetService<IThemeService>().ChangeTheme(isDarkTheme);
    }
#endif

    private static async void App_DocumentOpened(object? sender, DocumentOpenedEventArgs e)
    {
        GetService<IUserInterfaceActivityStream>().Publish(new DocumentOpenedActivity());
        await AddProjectParametersSafely(e.Document);
    }

    private static void App_DocumentClosing(object? sender, DocumentClosingEventArgs e)
    {
        try
        {
            GetService<TutorialSampleOpenRequest>().TrackClosing(e.DocumentId, e.Document.PathName);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to track a closing tutorial document");
        }
    }

    private static void App_DocumentClosed(object? sender, DocumentClosedEventArgs e)
    {
        try
        {
            if (!GetService<TutorialSampleOpenRequest>().TryReleaseClosed(e.DocumentId, out string? path)
                || path is null) return;

            GetService<ITutorialSampleCopyService>().DeleteCopy(path, ConfigurationPathUtils.RevitVersion);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to release a closed tutorial document");
        }
    }

    private static async void App_DocumentCreated(object? sender, DocumentCreatedEventArgs e)
    {
        GetService<IUserInterfaceActivityStream>().Publish(new DocumentOpenedActivity());
        await AddProjectParametersSafely(e.Document);
    }

    private static async Task AddProjectParametersSafely(Document document)
    {
        try
        {
            await AddIgnoreListParameters(document);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to configure project parameters");
            await GetService<IDialog>().Show(new DialogRequest(
                Localizer["projectParameters_configurationFailed_message", exception.Message]));
        }
    }

    private static async Task AddIgnoreListParameters(Document doc)
    {
        var projectParameterProvider = GetService<IProjectParameterProvider>();

        await RevitTask.RunAsync(() =>
        {
            var categories = doc.Settings.Categories.Cast<Category>()
                .Where(i => i.AllowsBoundParameters).Select(i => i.BuiltInCategory).ToList();
#if BEFORE2024
            var group = BuiltInParameterGroup.PG_IDENTITY_DATA;
#else
            var group = GroupTypeId.IdentityData;
#endif
            var ignoreInstanceId = new Guid("666a739a-ae5d-48d1-b146-fc0b2d7f5a4b");
            var ignoreTypeId = new Guid("e1c4d22f-9147-49d5-b7cc-6f13b35e4d53");

            // The check is read-only, so a document that is already configured
            // leaves no transaction behind in the Revit undo stack.
            if (projectParameterProvider.IsConfigured(doc, ignoreInstanceId, categories, group,
                    isInstance: true, allowVaryBetweenGroups: true)
                && projectParameterProvider.IsConfigured(doc, ignoreTypeId, categories, group,
                    isInstance: false, allowVaryBetweenGroups: false))
            {
                Logger.LogDebug("Project ignore-list parameters are already configured in document {DocumentTitle}", doc.Title);
                return;
            }

            bool configured;
            using (Transaction transaction = new(doc, "Parameter project adding"))
            {
                transaction.Start();

                configured = projectParameterProvider.Add(
                    doc, ignoreInstanceId, categories, group,
                    isInstance: true, allowVaryBetweenGroups: true);
                configured &= projectParameterProvider.Add(
                    doc, ignoreTypeId, categories, group,
                    isInstance: false, allowVaryBetweenGroups: false);

                if (configured)
                    transaction.Commit();
                else
                    transaction.RollBack();
            }

            if (configured)
                Logger.LogInformation("Configured project ignore-list parameters in document {DocumentTitle}", doc.Title);
            else
                Logger.LogWarning("Project ignore-list parameters could not be configured in document {DocumentTitle}", doc.Title);
        });
    }

    private static void AddRibbonButtons(RibbonPanel panel)
    {
        panel.AddItem(CreateButton(
            "ShowAllPanesButton", "showAllPanes", typeof(ShowAllPanesCommand),
            new("Dockable panes", "Закрепляемые панели")));
        panel.AddItem(CreateButton(
            "OpenConfigurationFolderButton", "openConfigurationFolder", typeof(OpenConfigurationFolderCommand),
            new("Diagnostic configuration path button", "Кнопка папки конфигурации")));
        panel.AddStackedItems(
            CreateButton(
                "CheckForUpdatesButton", "checkForUpdates", typeof(CheckForUpdatesCommand),
                new("Check for updates button", "Кнопка проверки обновлений"), compact: true),
            CreateButton(
                "OpenSupportButton", "support", typeof(OpenSupportCommand),
                new("Support button", "Кнопка поддержки"), compact: true),
            CreateButton(
                "OpenSponsorButton", "sponsor", typeof(OpenSponsorCommand),
                new("Sponsor button", "Кнопка спонсорства"), compact: true));
        panel.AddItem(CreateButton(
            "ShowWelcomeButton", "showWelcome", typeof(ShowWelcomeCommand),
            new("Getting started button", "Кнопка начала работы")));
        panel.AddItem(CreateButton(
            "ShowPracticalTourButton", "showPracticalTour", typeof(ShowPracticalTourCommand),
            new("Practical tour", "Практическое обучение")));
        _tutorialSampleButton = (PushButton)panel.AddItem(CreateButton(
            "OpenTutorialSampleButton", "openTutorialSample", typeof(OpenTutorialSampleCommand),
            new("Getting started button", "Кнопка начала работы")));
        _tutorialSampleButton.Visible = false;
    }

    internal static void SetTutorialSampleButtonVisible(bool visible)
    {
        if (_tutorialSampleButton is not null) _tutorialSampleButton.Visible = visible;
    }

    /// <summary>
    /// Creates a ribbon button whose texts are the <c>_buttonText</c>, <c>_toolTip</c> and
    /// <c>_longDescription</c> resources that start with <paramref name="resourcePrefix"/>.
    /// </summary>
    /// <remarks>A compact button is meant for a stacked column: it has a 16-pixel image and no large one.</remarks>
    private static PushButtonData CreateButton(
        string name, string resourcePrefix, Type commandType, DocumentationPage helpPage, bool compact = false)
    {
        PushButtonData buttonData = new(
            name, Localizer[$"{resourcePrefix}_buttonText"], AssemblyPath, commandType.FullName)
        {
            ToolTip = Localizer[$"{resourcePrefix}_toolTip"],
            LongDescription = Localizer[$"{resourcePrefix}_longDescription"],
            Image = compact ? SmallIcon : Icon,
            ToolTipImage = Icon
        };
        if (!compact) buttonData.LargeImage = Icon;

        // F1 over the button opens its documentation page in the language of the current UI culture.
        buttonData.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, helpPage.GetUrl()));
        return buttonData;
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

    private void RegisterDockablePanes()
    {
        RegisterDockablePane<DiagnosticReportView>(
            DiagnosticReportPaneUtils.PaneId, "diagnosticReport_dockablePane_title");
        RegisterDockablePane<FixReportView>(
            FixReportPaneUtils.PaneId, "fixReport_dockablePane_title", DiagnosticReportPaneUtils.PaneId);
        RegisterDockablePane<DiagnosticListView>(
            DiagnosticListPaneUtils.PaneId, "diagnosticList_dockablePane_title", DiagnosticReportPaneUtils.PaneId);
        Application.RegisterDockablePane(
            PracticalTourPaneUtils.PaneId,
            Localizer["practicalTour_dockablePane_title"],
            new DockablePaneProvider(GetService<PracticalTourView>(), dockPosition: DockPosition.Right));
    }

    private void RegisterDockablePane<TView>(
        DockablePaneId paneId, string titleResourceKey, DockablePaneId? tabBehind = null)
        where TView : System.Windows.FrameworkElement
        => Application.RegisterDockablePane(
            paneId, Localizer[titleResourceKey], new DockablePaneProvider(GetService<TView>(), tabBehind));

    private static T GetService<T>() where T : notnull => Program.Provider.GetRequiredService<T>();
}
