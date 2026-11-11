using Autodesk.Revit.DB;
using Microsoft.Extensions.Logging.Abstractions;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using TUnit.Core.Executors;

namespace Revit.Linter.ElementVisualization.RevitTests;

public sealed class OpenViewVisualizationPipelineTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Apply_opens_the_target_view()
    {
        Document document = _document!;
        View targetView = GetTargetView(document);
        View? activatedView = null;
        OpenViewVisualizationPipeline pipeline = CreatePipeline();

        bool applied = pipeline.Apply(new ElementVisualizationContext(
            document, targetView, new Dictionary<string, IReadOnlyCollection<ElementId>>(), view => activatedView = view));

        await Assert.That(pipeline is ITargetViewVisualizationPipeline).IsTrue();
        await Assert.That(applied).IsTrue();
        await Assert.That(activatedView).IsNotNull();
        await Assert.That(activatedView!.Id).IsEqualTo(targetView.Id);
    }

    [Test]
    public async Task Restore_and_repeated_apply_leave_the_view_open()
    {
        Document document = _document!;
        View targetView = GetTargetView(document);
        List<ElementId> activatedViewIds = [];
        OpenViewVisualizationPipeline pipeline = CreatePipeline();

        bool firstApply = pipeline.Apply(CreateContext(document, targetView, activatedViewIds));
        pipeline.Restore();
        pipeline.Restore();
        bool secondApply = pipeline.Apply(CreateContext(document, targetView, activatedViewIds));

        await Assert.That(firstApply).IsTrue();
        await Assert.That(secondApply).IsTrue();
        await Assert.That(activatedViewIds.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Apply_declines_to_open_a_view_while_the_document_is_modifiable()
    {
        Document document = _document!;
        View targetView = GetTargetView(document);
        View? activatedView = null;
        OpenViewVisualizationPipeline pipeline = CreatePipeline();

        bool applied;
        using (Transaction transaction = new(document, "Keep the document modifiable"))
        {
            transaction.Start();
            try
            {
                applied = pipeline.Apply(new ElementVisualizationContext(
                    document, targetView, new Dictionary<string, IReadOnlyCollection<ElementId>>(),
                    view => activatedView = view));
            }
            finally
            {
                transaction.RollBack();
            }
        }

        await Assert.That(applied).IsTrue();
        await Assert.That(activatedView).IsNull();
    }

    private static View GetTargetView(Document document) => new FilteredElementCollector(document)
        .OfClass(typeof(View))
        .Cast<View>()
        .First(view => !view.IsTemplate);

    private static ElementVisualizationContext CreateContext(
        Document document, View targetView, List<ElementId> activatedViewIds) =>
        new(
            document,
            targetView,
            new Dictionary<string, IReadOnlyCollection<ElementId>>(),
            view => activatedViewIds.Add(view.Id));

    private static OpenViewVisualizationPipeline CreatePipeline() => new(
        new ElementDiagnosticId(
            "TEST", "Test", "Test", DiagnosticSeverity.Message, true, false, string.Empty),
        "Open view",
        NullLogger<OpenViewVisualizationPipeline>.Instance);
}
