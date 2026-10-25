using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Revit.Linter.CollisionTestModels.Abstractions.Models;
using Revit.Linter.CollisionTestModels.Infrastructure.Services;

namespace Revit.Linter.CollisionTestModels;

/// <summary>
/// Builds collision-testing documents: a host with its own wall and two links placed by shared
/// coordinates whose walls overlap the host wall.
/// </summary>
/// <remarks>
/// A thin UI adapter over <c>CollisionTestModelService</c>: it extracts the Revit application
/// object and reports the outcome, while all document work stays in the RevitAPI-only service.
/// After the documents are created it offers to open one of them for testing; opening stays
/// in this adapter so the service remains free of <c>RevitAPIUI</c>.
/// </remarks>
[Transaction(TransactionMode.Manual)]
public sealed class CreateCollisionTestModelsCommand : IExternalCommand
{
    /// <inheritdoc />
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            UIApplication uiApplication = commandData.Application;
            CollisionTestModelResult result = new CollisionTestModelService()
                .CreateHostWithTwoLinks(uiApplication.Application);

            TaskDialogResult choice = AskWhichModelToOpen(result);
            string? pathToOpen = choice switch
            {
                TaskDialogResult.CommandLink1 => result.HostPath,
                TaskDialogResult.CommandLink2 => result.LinkAPath,
                TaskDialogResult.CommandLink3 => result.LinkBPath,
                _ => null,
            };

            // NewProjectDocument leaves the host in the background, so every choice opens
            // its document: opening an already open document activates it. Closing the dialog
            // stays in the current document.
            if (pathToOpen is not null && !TryOpenModelForTesting(uiApplication, pathToOpen, out string? openError))
            {
                message = openError ?? string.Empty;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
        catch (Exception exception)
        {
            message = exception.Message;
            return Result.Failed;
        }
    }

    private static TaskDialogResult AskWhichModelToOpen(CollisionTestModelResult result)
    {
        using TaskDialog dialog = new("Collision test models");
        dialog.MainInstruction = $"Host '{result.HostTitle}' created with two shared-coordinates links.";
        dialog.MainContent = $"Host: {result.HostPath}\nLink A: {result.LinkAPath}\nLink B: {result.LinkBPath}\n\n"
            + "Run a collision rule with link checks enabled to see host-versus-link hits.\n"
            + "Which model do you want to open for testing?";
        dialog.AddCommandLink(
            TaskDialogCommandLinkId.CommandLink1,
            "Stay in the host",
            "Open the host with both links loaded.");
        dialog.AddCommandLink(
            TaskDialogCommandLinkId.CommandLink2,
            "Open Link A",
            "Open the first link model whose wall overlaps the host wall.");
        dialog.AddCommandLink(
            TaskDialogCommandLinkId.CommandLink3,
            "Open Link B",
            "Open the second link model whose wall overlaps the host wall.");
        dialog.CommonButtons = TaskDialogCommonButtons.Close;
        dialog.DefaultButton = TaskDialogResult.CommandLink1;
        return dialog.Show();
    }

    private static bool TryOpenModelForTesting(UIApplication uiApplication, string path, out string? error)
    {
        try
        {
            uiApplication.OpenAndActivateDocument(path);
            error = null;
            return true;
        }
        catch (Exception exception) when (exception is IOException or Autodesk.Revit.Exceptions.ApplicationException)
        {
            error = exception.Message;
            return false;
        }
    }
}
