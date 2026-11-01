using Autodesk.Revit.DB;
using Microsoft.Extensions.Logging.Abstractions;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Services;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.Languages.Factories;
using Revit.Linter.Testing;
using TUnit.Core.Executors;
using CollisionElementDiagnostic = Revit.Linter.CollisionDiagnostics.ElementDiagnostic;

namespace Revit.Linter.CollisionDiagnostics.RevitTests;

/// <summary>
/// Covers the finding a collision rule produces when Revit rejects the solid intersection: the pair is
/// reported as unverified with its own message, a computed overlap still uses the rule message, and a
/// compared pair without overlap stays valid. The volume calculation is replaced by the test seam,
/// because touching geometry does not make Revit reject the operation deterministically.
/// </summary>
public sealed class UnverifiedCollisionTests : RevitApiTest
{
    private const string RuleMessage = "Rule message: {elementName}";
    private const double OverlapVolume = 1.0;

    private Document? _document;
    private Wall? _target;
    private Wall? _candidate;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateModel()
    {
        _document = Application.NewProjectDocument(UnitSystem.Metric);
        using Transaction transaction = new(_document, "Create collision walls");
        transaction.Start();
        Level level = Level.Create(_document, 0);
        // The walls cross in plan, so the pair passes the bounding-box check; the solid comparison
        // itself is replaced by the test seam, so the join Revit creates does not affect the result.
        _target = Wall.Create(
            _document, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), level.Id, false);
        _candidate = Wall.Create(
            _document, Line.CreateBound(new XYZ(5, -5, 0), new XYZ(5, 5, 0)), level.Id, false);
        transaction.Commit();
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseModel()
    {
        _document?.Close(false);
        _document = null;
    }

    [Test]
    public async Task Rejected_boolean_operation_is_reported_as_unverified()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic((_, _) => null);

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, _target!);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(feedback.MessageFormat)
            .IsEqualTo(CollisionDiagnosticLocalizations.GetString("Unverified_message"));
        await Assert.That(GetArgument<int>(feedback, "intersection.count")).IsEqualTo(1);
        await Assert.That(GetArgument<string>(feedback, "intersection.elementNames"))
            .Contains(_candidate!.Name);
        Element dependency = feedback.AdditionalTargetDependencies.OfType<Element>().Single();
        await Assert.That(dependency.Id.Equals(_candidate.Id)).IsTrue();
    }

    [Test]
    public async Task Computed_overlap_uses_the_rule_message()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic((_, _) => OverlapVolume);

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, _target!);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(feedback.MessageFormat).IsNull();
        await Assert.That(GetArgument<string>(feedback, "intersection.elementNames"))
            .Contains(_candidate!.Name);
    }

    [Test]
    public async Task Compared_pair_without_overlap_is_valid()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic((_, _) => 0);

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, _target!);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    private static T GetArgument<T>(DiagnosticFeedback feedback, string name) =>
        (T)feedback.AdditionalMessageArguments![name];

    private static CollisionElementDiagnostic CreateDiagnostic(
        Func<Solid, Solid, double?> intersectionVolume)
    {
        // A new query service per diagnostic: the test cache never invalidates on transactions.
        IDocumentQueryService queries = TestDocumentQueries.Create();
        FormulaCompilationNotifier notifier = new();
        return new CollisionElementDiagnostic(
            new ElementFilterFactory(NullLogger<ElementFilterFactory>.Instance, notifier),
            new ElementFunctionFactory(NullLogger<ElementFunctionFactory>.Instance, notifier),
            new GetElementBoundingBoxService(queries),
            new GetElementGeometryService(queries),
            new GetConnectedElementIdsService(queries),
            queries,
            NullLogger<CollisionElementDiagnostic>.Instance)
        {
            Identity = new ElementDiagnosticId(
                "CLSN-TEST", "Description", RuleMessage, DiagnosticSeverity.Warning, true, false, ""),
            TakeFormula = "instance and (builtincategory('OST_Walls'))",
            GroupByFormula = "'all'",
            IntersectionVolume = intersectionVolume,
        };
    }

    private sealed class FormulaCompilationNotifier : IFormulaCompilationNotifier
    {
        public void Notify()
        {
            // Compilation failures are not part of what these tests assert.
        }
    }
}
