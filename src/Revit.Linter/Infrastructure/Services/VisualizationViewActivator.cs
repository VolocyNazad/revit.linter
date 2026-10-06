using Microsoft.Extensions.Logging;
using Revit.Context.Abstractions.Services;
using Revit.Linter.DiagnosticReportPresenter.Interactions.Abstractions.Services;

namespace Revit.Linter.Infrastructure.Services;

internal sealed class VisualizationViewActivator(
    IRevitContext revitContext,
    ILogger<VisualizationViewActivator> logger) : IVisualizationViewActivator
{
    private const string VisualizationViewName = "Revit Linter — Visualization";

    public void Activate()
    {
        UIDocument? uiDocument = revitContext.UIApplication?.ActiveUIDocument;
        if (uiDocument is null)
            throw new InvalidOperationException("No project document is active.");

        Document document = uiDocument.Document;
        if (document.IsFamilyDocument)
            throw new InvalidOperationException("Diagnostic visualization requires a project document.");

        View3D? existingView = FindVisualizationView(document);
        View3D view = existingView ?? CreateVisualizationView(document);
        if (existingView is null)
            logger.LogInformation(
                "Created diagnostic visualization view {ViewName} in document {DocumentTitle}",
                view.Name, document.Title);
        if (!uiDocument.ActiveView.Id.Equals(view.Id))
        {
            uiDocument.ActiveView = view;
            logger.LogDebug(
                "Activated diagnostic visualization view {ViewName} in document {DocumentTitle}",
                view.Name, document.Title);
        }

    }

    private static View3D? FindVisualizationView(Document document) =>
        new FilteredElementCollector(document)
            .OfClass(typeof(View3D))
            .Cast<View3D>()
            .FirstOrDefault(view =>
                !view.IsTemplate &&
                view.Name.StartsWith(VisualizationViewName, StringComparison.Ordinal));

    private static View3D CreateVisualizationView(Document document)
    {
        ViewFamilyType? viewFamilyType = new FilteredElementCollector(document)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(type => type.ViewFamily == ViewFamily.ThreeDimensional);
        if (viewFamilyType is null)
            throw new InvalidOperationException("The document does not contain a three-dimensional view type.");

        using var transaction = new Transaction(document, "Create Revit Linter visualization view");
        transaction.Start();
        View3D view = View3D.CreateIsometric(document, viewFamilyType.Id);
        view.Name = FindAvailableName(document);
        transaction.Commit();
        return view;
    }

    private static string FindAvailableName(Document document)
    {
        HashSet<string> names = new FilteredElementCollector(document)
            .OfClass(typeof(View))
            .Cast<View>()
            .Select(view => view.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!names.Contains(VisualizationViewName)) return VisualizationViewName;

        int suffix = 2;
        while (names.Contains($"{VisualizationViewName} ({suffix})")) suffix++;
        return $"{VisualizationViewName} ({suffix})";
    }
}
