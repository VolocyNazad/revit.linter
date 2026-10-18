using RevitPerformanceAdviser = Autodesk.Revit.DB.PerformanceAdviser;

namespace Revit.Linter.DocumentDiagnostics.Diagnostics.PerformanceAdviser;

internal sealed class PerformanceAdviserDiagnostic : IDocumentDiagnostic
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.PerformanceAdviser;

    public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument)
    {
        RevitPerformanceAdviser adviser = RevitPerformanceAdviser.GetPerformanceAdviser();
        // Every enabled rule is reported, including "unused families and types", which also
        // surfaces as the RVT003 element diagnostic: the overlap is intentional.
        foreach (PerformanceAdviserRuleId ruleId in adviser.GetAllRuleIds())
        {
            // ExecuteRules runs a rule regardless of its enabled flag, so honor the setting
            // that the Performance Adviser dialog (and ExecuteAllRules) expose to the user.
            if (!adviser.IsRuleEnabled(ruleId)) continue;

            IList<FailureMessage> messages;
            try
            {
                messages = adviser.ExecuteRules(targetDocument, new List<PerformanceAdviserRuleId> { ruleId });
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                // A rule can reject a document shape it does not support. An unreadable state
                // counts as clean so one rule cannot fail the whole run.
                continue;
            }

            string ruleName = adviser.GetRuleName(ruleId);
            foreach (FailureMessage message in messages)
            {
                yield return new DiagnosticFeedback(
                    DiagnosticVerdict.NotValid,
                    new Dictionary<string, object>
                    {
                        ["ruleName"] = ruleName,
                        ["elementIds"] = message.GetFailingElements(),
                        ["details"] = message.GetDescriptionText(),
                    });
            }
        }
    }
}
