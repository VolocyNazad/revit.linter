using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DocumentDiagnostics.Diagnostics.StartingViewNotSet;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class StartingViewTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Missing_starting_view_is_reported()
    {
        Document document = _document!;
        using (Transaction transaction = new(document, "Clear test starting view"))
        {
            transaction.Start();
            StartingViewSettings.GetStartingViewSettings(document).ViewId = ElementId.InvalidElementId;
            transaction.Commit();
        }
        var diagnostic = new StartingViewNotSetDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Assigned_starting_view_is_valid()
    {
        Document document = _document!;
        ElementId viewId;
        using (Transaction transaction = new(document, "Assign test starting view"))
        {
            transaction.Start();
            ViewSchedule schedule = ViewSchedule.CreateSchedule(
                document, new ElementId(BuiltInCategory.OST_Walls));
            viewId = schedule.Id;
            StartingViewSettings settings = StartingViewSettings.GetStartingViewSettings(document);
            if (!settings.IsAcceptableStartingView(viewId))
                throw new InvalidOperationException(
                    $"The test schedule '{schedule.Name}' is not an acceptable starting view.");
            settings.ViewId = viewId;
            transaction.Commit();
        }
        var diagnostic = new StartingViewNotSetDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document).Single();

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }
}
