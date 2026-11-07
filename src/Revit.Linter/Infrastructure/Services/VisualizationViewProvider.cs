using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Revit.Context.Abstractions.Services;
using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.Infrastructure.Services;

internal sealed class VisualizationViewProvider(
    IRevitContext revitContext,
    IStringLocalizer<GlobalLocalizations> localizer,
    ILogger<VisualizationViewProvider> logger) : IVisualizationViewProvider
{
    private const string VisualizationViewName = "Revit Linter — Visualization";

    // A large model regenerates this view on every activation, so it opens in the cheapest useful
    // mode: coarse geometry and shaded surfaces. A pipeline can still change the detail level or the
    // display style with its own steps.
    private const ViewDetailLevel VisualizationDetailLevel = ViewDetailLevel.Coarse;
    private const DisplayStyle VisualizationDisplayStyle = DisplayStyle.Shading;

    public View3D GetOrCreateView()
    {
        UIDocument? uiDocument = revitContext.UIApplication?.ActiveUIDocument;
        if (uiDocument is null)
            throw new InvalidOperationException("No project document is active.");

        Document document = uiDocument.Document;
        if (document.IsFamilyDocument)
            throw new InvalidOperationException("Diagnostic visualization requires a project document.");

        View3D? existingView = FindVisualizationView(document);
        if (existingView is not null)
        {
            PrepareVisualizationView(document, existingView);
            return existingView;
        }

        View3D view = CreateVisualizationView(document);
        logger.LogInformation(
            "Created diagnostic visualization view {ViewName} in document {DocumentTitle}",
            view.Name, document.Title);
        return view;
    }

    public void Activate(View view)
    {
        UIDocument? uiDocument = revitContext.UIApplication?.ActiveUIDocument;
        if (uiDocument is null || !uiDocument.Document.Equals(view.Document))
        {
            logger.LogWarning(
                "The visualization view {ViewName} was not activated because it does not belong " +
                "to the active document",
                view.Name);
            return;
        }

        if (uiDocument.ActiveView is { } activeView && activeView.Id.Equals(view.Id)) return;
        uiDocument.ActiveView = view;
        logger.LogDebug(
            "Activated diagnostic visualization view {ViewName} in document {DocumentTitle}",
            view.Name, uiDocument.Document.Title);
    }

    private static View3D? FindVisualizationView(Document document) =>
        new FilteredElementCollector(document)
            .OfClass(typeof(View3D))
            .Cast<View3D>()
            .FirstOrDefault(view =>
                !view.IsTemplate &&
                view.Name.StartsWith(VisualizationViewName, StringComparison.Ordinal));

    private View3D CreateVisualizationView(Document document)
    {
        ViewFamilyType? viewFamilyType = new FilteredElementCollector(document)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(type => type.ViewFamily == ViewFamily.ThreeDimensional);
        if (viewFamilyType is null)
            throw new InvalidOperationException("The document does not contain a three-dimensional view type.");

        using var transaction = new Transaction(document, localizer["visualizationView_create_transaction"]);
        transaction.Start();
        View3D view = View3D.CreateIsometric(document, viewFamilyType.Id);
        view.Name = FindAvailableName(document);
        ApplyVisualizationViewSettings(view);
        transaction.Commit();
        return view;
    }

    // A view created by an earlier version keeps its original settings. They are rewritten once, so
    // an already open visualization view becomes as cheap to regenerate as a newly created one.
    private void PrepareVisualizationView(Document document, View3D view)
    {
        if (view.DetailLevel == VisualizationDetailLevel
            && view.DisplayStyle == VisualizationDisplayStyle)
            return;

        using var transaction = new Transaction(document, localizer["visualizationView_prepare_transaction"]);
        transaction.Start();
        ApplyVisualizationViewSettings(view);
        transaction.Commit();
        logger.LogInformation(
            "Prepared diagnostic visualization view {ViewName} in document {DocumentTitle} for " +
            "{DetailLevel} detail and {DisplayStyle} display style",
            view.Name, document.Title, VisualizationDetailLevel, VisualizationDisplayStyle);
    }

    private static void ApplyVisualizationViewSettings(View3D view)
    {
        view.DetailLevel = VisualizationDetailLevel;
        view.DisplayStyle = VisualizationDisplayStyle;
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
