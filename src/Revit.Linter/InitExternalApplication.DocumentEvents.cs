// Revit document and view events that publish UI activities and maintain tutorial copies.

using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI.Events;
using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.Infrastructure.Services;
using Revit.Linter.WelcomePresenter.Abstractions;
using Revit.Linter.WelcomePresenter.Abstractions.Services;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.ConfigurationPath.Abstractions.Services;
#if BEFORE2024
using Revit.Sugar;
#endif

namespace Revit.Linter;

internal sealed partial class InitExternalApplication
{
    private static void App_DocumentOpened(object? sender, DocumentOpenedEventArgs e)
    {
        GetService<IUserInterfaceActivityStream>().Publish(new DocumentOpenedActivity());
        // Revit restores the previous pane layout when a document opens: hide the tour pane
        // here, after the tour observed the document, when no session fills it. An active
        // session (for example waiting for a document) keeps its pane.
        GetService<IPracticalTour>().HidePaneIfInactive();
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

            GetService<ITutorialSampleCopyService>().DeleteCopy(
                path, GetService<IConfigurationPaths>().RevitVersion);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to release a closed tutorial document");
        }
    }

    private static void App_ViewActivated(object? sender, ViewActivatedEventArgs e)
    {
        if (e.CurrentActiveView is null || e.PreviousActiveView is null) return;
        if (e.CurrentActiveView.Document.Equals(e.PreviousActiveView.Document)) return;
        GetService<IUserInterfaceActivityStream>().Publish(new DocumentFocusChangedActivity());
    }

    private static void App_DocumentCreated(object? sender, DocumentCreatedEventArgs e)
    {
        GetService<IUserInterfaceActivityStream>().Publish(new DocumentOpenedActivity());
        // Same restored-layout case as for an opened document: see App_DocumentOpened.
        GetService<IPracticalTour>().HidePaneIfInactive();
    }
}

