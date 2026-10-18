using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DocumentDiagnostics.Diagnostics.PerformanceAdviser;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class PerformanceAdviserTests : RevitApiTest
{
    // The "unused families and types" rule is reported as the RVT003 element diagnostic.
    private static readonly Guid PurgeableElementsRuleGuid = new("e8c63650-70b7-435a-9010-ec97660c1bda");

    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Empty_project_reports_findings_with_rule_details()
    {
        var diagnostic = new PerformanceAdviserDiagnostic();

        DiagnosticFeedback[] feedbacks = diagnostic.Execute(_document!).ToArray();

        await Assert.That(feedbacks).IsNotEmpty();
        foreach (DiagnosticFeedback feedback in feedbacks)
        {
            await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
            Dictionary<string, object> arguments = feedback.AdditionalMessageArguments!;
            await Assert.That((string)arguments["ruleName"]).IsNotEmpty();
            await Assert.That((string)arguments["details"]).IsNotEmpty();
            await Assert.That((ICollection<ElementId>)arguments["elementIds"]).IsNotNull();
        }
    }

    // The rule overlaps the RVT003 element diagnostic; RVT002 reports it too, so the empty
    // project (which trips it) must surface it among the findings.
    [Test]
    public async Task Purgeable_elements_rule_is_reported()
    {
        PerformanceAdviser adviser = PerformanceAdviser.GetPerformanceAdviser();
        PerformanceAdviserRuleId purgeableRule = adviser.GetAllRuleIds()
            .Single(ruleId => ruleId.Guid == PurgeableElementsRuleGuid);
        IList<FailureMessage> rawMessages = adviser.ExecuteRules(
            _document!, [purgeableRule]);
        string purgeableRuleName = adviser.GetRuleName(purgeableRule);
        var diagnostic = new PerformanceAdviserDiagnostic();

        DiagnosticFeedback[] feedbacks = diagnostic.Execute(_document!).ToArray();

        await Assert.That(rawMessages).IsNotEmpty();
        await Assert.That(feedbacks.Any(feedback =>
            (string)feedback.AdditionalMessageArguments!["ruleName"] == purgeableRuleName)).IsTrue();
    }

    // ExecuteRules ignores the enabled flag, so the wrapper has to check it: with every rule
    // disabled the empty project, which always trips a rule, must produce no findings.
    [Test]
    public async Task Disabled_rules_are_not_reported()
    {
        PerformanceAdviser adviser = PerformanceAdviser.GetPerformanceAdviser();
        PerformanceAdviserRuleId[] ruleIds = adviser.GetAllRuleIds().ToArray();
        bool[] originalStates = ruleIds.Select(adviser.IsRuleEnabled).ToArray();
        try
        {
            foreach (PerformanceAdviserRuleId ruleId in ruleIds)
                adviser.SetRuleEnabled(ruleId, false);
            var diagnostic = new PerformanceAdviserDiagnostic();

            DiagnosticFeedback[] feedbacks = diagnostic.Execute(_document!).ToArray();

            await Assert.That(feedbacks).IsEmpty();
        }
        finally
        {
            for (int index = 0; index < ruleIds.Length; index++)
                adviser.SetRuleEnabled(ruleIds[index], originalStates[index]);
        }
    }

    [Test]
    public async Task Filter_is_relevant_for_projects_and_families()
    {
        var filter = new PerformanceAdviserDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(_document!)).IsTrue();
        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(filter.IsRelevantFor(familyDocument)).IsTrue();
        }
        finally
        {
            familyDocument.Close(false);
        }
    }
}
