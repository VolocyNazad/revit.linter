using Revit.Linter.CollisionTestModels.Abstractions.Models;

namespace Revit.Linter.CollisionTestModels.Abstractions.Services;

/// <summary>
/// Builds collision-testing documents: a host with its own walls, floors and system runs
/// and two linked models whose content lands on the host content through a shared-coordinates site.
/// </summary>
/// <remarks>
/// The contract uses RevitAPI types only (<c>Autodesk.Revit.ApplicationServices.Application</c>
/// and <c>Document</c>), so document logic stays testable without the Revit UI.
/// Showing dialogs and extracting the application object remain the caller's responsibility.
/// </remarks>
public interface ICollisionTestModelService
{
    /// <summary>
    /// Creates two saved link models and a saved host with both links placed by shared coordinates.
    /// </summary>
    /// <param name="application">The Revit application used to create the documents.</param>
    /// <returns>Titles and file paths of the created documents.</returns>
    CollisionTestModelResult CreateHostWithTwoLinks(Autodesk.Revit.ApplicationServices.Application application);
}
