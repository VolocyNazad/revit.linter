using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using System.Diagnostics;

namespace Revit.Linter.ElementVisualization.Services;

/// <summary>
/// Colours diagnostic target elements by the severity and the number of their failed checks.
/// </summary>
/// <remarks>
/// The map uses the same fixed colour table as the report-pane legend: one hue per severity and a
/// brightness that grows with the number of distinct checks. Elements without model geometry or
/// elements that the view does not display are skipped instead of failing the whole map. The colour of
/// each step and the nearly transparent background are applied as reversible per-element overrides, so
/// the coloured findings are opaque and stand out from the model.
/// </remarks>
internal sealed class ElementHeatmapService(
    IOverrideElementGraphicsService overrideElementGraphicsService,
    ISetCategoryVisibilityService setCategoryVisibilityService,
    IDocumentQueryService documentQueries,
    ILogger<ElementHeatmapService> logger) : IElementHeatmapService
{
    // The background keeps only a faint trace of the model: enough to keep the context readable.
    private const int UncoloredTransparency = 99;

    // Levels, scope boxes and grids only clutter the isometric view and are never diagnostic targets.
    // Linked instances are hidden too unless a finding targets one of them directly.
    private static readonly BuiltInCategory[] AlwaysHiddenCategories =
    [
        BuiltInCategory.OST_Levels,
        BuiltInCategory.OST_VolumeOfInterest,
        BuiltInCategory.OST_Grids,
    ];

    /// <inheritdoc />
    public IElementHeatmapSession Apply(
        Document document,
        View view,
        IReadOnlyList<FindingHeatmapEntry> entries)
    {
        if (entries.Count == 0) return new ElementHeatmapSession(document, [], 0, 0, logger);

        if (!view.AreGraphicsOverridesAllowed())
            throw new InvalidOperationException(
                $"Graphics overrides are not allowed in view '{view.Name}'.");

        Stopwatch stopwatch = Stopwatch.StartNew();
        IList<Element> displayedElements = new FilteredElementCollector(document, view.Id)
            .WhereElementIsNotElementType()
            .ToElements();
        HashSet<ElementId> displayedElementIds = displayedElements.Select(element => element.Id).ToHashSet();

        Dictionary<ElementId, FindingHeatmapEntry> values = Normalize(entries);
        int skippedCount = 0;
        Dictionary<(DiagnosticSeverity Severity, int Level), List<ElementId>> groups = [];
        HashSet<ElementId> coloredElementIds = [];
        foreach (FindingHeatmapEntry entry in values.Values)
        {
            if (!CanBeColored(document, view, entry.ElementId, displayedElementIds))
            {
                skippedCount++;
                continue;
            }

            (DiagnosticSeverity Severity, int Level) key =
                (entry.Severity, FindingHeatmapScale.GetLevel(entry.Count));
            if (!groups.TryGetValue(key, out List<ElementId>? elementIds))
            {
                elementIds = [];
                groups[key] = elementIds;
            }

            elementIds.Add(entry.ElementId);
            coloredElementIds.Add(entry.ElementId);
        }

        int coloredCount = coloredElementIds.Count;
        if (groups.Count == 0)
        {
            logger.LogInformation(
                "The findings heat map has no elements to colour in document {DocumentTitle}: " +
                "{SkippedCount} elements were skipped",
                document.Title, skippedCount);
            return new ElementHeatmapSession(document, [], 0, skippedCount, logger);
        }

        // Every other visible model element becomes the transparent background. Per-element overrides
        // keep the coloured findings completely opaque: their own override never inherits the
        // transparency of a category or a neighbouring element.
        List<ElementId> uncoloredElementIds = displayedElements
            .Where(element => !coloredElementIds.Contains(element.Id))
            .Where(element => element.Category is { CategoryType: CategoryType.Model })
            .Select(element => element.Id)
            .ToList();

        using TransactionGroup? transactionGroup = VisualizationTransactions.StartGroup(
            document, ElementVisualizationLocalizations.GetString("heatmapApply_transactionGroup"));
        List<IRestoreSession> sessions = [];
        try
        {
            // Levels, scope boxes and grids are hidden for the duration of the map. A linked model is
            // hidden as well unless one of its instances is a finding target: linked content cannot be
            // coloured, so an untargeted link only hides the findings.
            List<BuiltInCategory> hiddenCategories = [.. AlwaysHiddenCategories];
            if (!HasLinkedInstanceTarget(document, values))
                hiddenCategories.Add(BuiltInCategory.OST_RvtLinks);
            sessions.Add(setCategoryVisibilityService.Apply(
                document, view, hiddenCategories, CategoryVisibilityMode.Hide));

            if (uncoloredElementIds.Count > 0)
            {
                sessions.Add(overrideElementGraphicsService.Apply(
                    document, view, uncoloredElementIds, CreateUncoloredStyle(view)));
            }

            foreach (KeyValuePair<(DiagnosticSeverity Severity, int Level), List<ElementId>> group in groups)
            {
                FindingHeatmapColor color =
                    FindingHeatmapScale.GetColor(group.Key.Severity, CountOfLevel(group.Key.Level));
                sessions.Add(overrideElementGraphicsService.Apply(
                    document, view, group.Value, CreateStyle(color)));
            }

            VisualizationTransactions.Assimilate(transactionGroup);

            logger.LogInformation(
                "Applied the findings heat map in view {ViewName} of document {DocumentTitle}: " +
                "{ColoredCount} elements in {GroupCount} colour groups, {UncoloredCount} background " +
                "elements, {SkippedCount} elements skipped, {ElapsedMilliseconds:F0} ms",
                view.Name, document.Title, coloredCount, groups.Count, uncoloredElementIds.Count,
                skippedCount, stopwatch.Elapsed.TotalMilliseconds);
            return new ElementHeatmapSession(document, sessions, coloredCount, skippedCount, logger);
        }
        catch (Exception exception)
        {
            VisualizationTransactions.RollBack(transactionGroup);
            DisposeSessions(sessions);
            logger.LogError(
                exception,
                "Failed to apply the findings heat map in view {ViewName} of document {DocumentTitle}",
                view.Name, document.Title);
            throw new InvalidOperationException(
                $"The findings heat map could not be applied in view '{view.Name}'.", exception);
        }
    }

    // The colour of a group is decided by the count band, so every level of a severity gets its own
    // style. A count inside a band maps to the same style as the band's first count.
    private static int CountOfLevel(int level) => FindingHeatmapScale.CountBands[level].MinimumCount;

    private static ViewGraphicsStyle CreateStyle(FindingHeatmapColor color)
    {
        Color revitColor = new(color.Red, color.Green, color.Blue);
        return new ViewGraphicsStyle
        {
            Halftone = false,
            Transparency = 0,
            ProjectionLines = new LineGraphicsStyle { Color = revitColor },
            SurfaceForeground = new PatternGraphicsStyle
            {
                Color = revitColor,
                PatternName = "SolidFill",
                IsVisible = true,
            },
            SurfaceBackground = new PatternGraphicsStyle
            {
                Color = revitColor,
                PatternName = "SolidFill",
                IsVisible = true,
            },
        };
    }

    // The background surfaces become almost invisible through transparency, but line graphics have no
    // transparency. Their colour is set to the view background instead, so pipe, duct and other
    // centre lines fade into the background with the surfaces.
    private static ViewGraphicsStyle CreateUncoloredStyle(View view)
    {
        Color lineColor = GetBackgroundLineColor(view);
        return new ViewGraphicsStyle
        {
            Halftone = true,
            Transparency = UncoloredTransparency,
            ProjectionLines = new LineGraphicsStyle { Color = lineColor, Weight = 1 },
            CutLines = new LineGraphicsStyle { Color = lineColor },
        };
    }

    private static Color GetBackgroundLineColor(View view)
    {
        try
        {
            using ViewDisplayBackground background = view.GetBackground();
            if (background.BackgroundColor is { IsValid: true } color)
                return new Color(color.Red, color.Green, color.Blue);
        }
        catch (Exception)
        {
            // Some views do not expose a background; the light fallback still keeps the lines faint.
        }

        return new Color(235, 235, 235);
    }

    // The aggregation already returns one value per element; this normalization only guards the public
    // service boundary against callers that repeat an element and keeps the worst value.
    private static Dictionary<ElementId, FindingHeatmapEntry> Normalize(
        IReadOnlyList<FindingHeatmapEntry> entries)
    {
        Dictionary<ElementId, FindingHeatmapEntry> values = [];
        foreach (FindingHeatmapEntry entry in entries)
        {
            if (!values.TryGetValue(entry.ElementId, out FindingHeatmapEntry? current)
                || entry.Severity > current.Severity
                || (entry.Severity == current.Severity && entry.Count > current.Count))
            {
                values[entry.ElementId] = entry;
            }
        }

        return values;
    }

    // Linked content is recoloured per instance as a whole, so a link instance only stays visible when a
    // finding targets that instance itself.
    private static bool HasLinkedInstanceTarget(
        Document document,
        IReadOnlyDictionary<ElementId, FindingHeatmapEntry> values) =>
        values.Keys.Any(elementId => document.GetElement(elementId) is RevitLinkInstance);

    private static void DisposeSessions(IEnumerable<IRestoreSession> sessions)
    {
        foreach (IRestoreSession session in sessions.Reverse())
        {
            try
            {
                session.Dispose();
            }
            catch (Exception)
            {
                // The session has already logged the failure; disposal is best-effort.
            }
        }
    }

    private bool CanBeColored(
        Document document,
        View view,
        ElementId elementId,
        HashSet<ElementId> displayedElements)
    {
        Element? element = document.GetElement(elementId);
        if (element is null or ElementType) return false;
        if (element.Category is not { CategoryType: CategoryType.Model }) return false;
        if (!displayedElements.Contains(elementId)) return false;

        GeometryElement? geometry = documentQueries.GetGeometry(element, view);
        return geometry is not null && geometry.Any();
    }
}
