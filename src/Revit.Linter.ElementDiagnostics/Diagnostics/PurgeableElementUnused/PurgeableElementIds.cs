namespace Revit.Linter.ElementDiagnostics.Diagnostics.PurgeableElementUnused;

// Purge oracle with a version split: Revit 2024 and later exposes the Purge Unused list
// directly through Document.GetUnusedElements, while older versions go through the
// "unused families and types" PerformanceAdviser rule (Matt Taylor's approach from
// The Building Coder "Purge Unused using Performance Adviser").
// NOTE: AFTER2023 means >= 2023, while GetUnusedElements exists only in 2024-2027,
// so the split must use AFTER2024.
internal static class PurgeableElementIds
{
#if !AFTER2024
    private static readonly PerformanceAdviserRuleId UnusedFamiliesAndTypesRuleId =
        new(new Guid("e8c63650-70b7-435a-9010-ec97660c1bda"));
#endif

    public static HashSet<ElementId> GetIds(Document document)
    {
#if AFTER2024
        return new HashSet<ElementId>(document.GetUnusedElements(new HashSet<ElementId>()));
#else
        return GetIdsByPerformanceRule(document);
#endif
    }

#if !AFTER2024
    // The rule is internal and may be missing: an empty set keeps every element valid
    // instead of failing the whole diagnostic run.
    private static HashSet<ElementId> GetIdsByPerformanceRule(Document document)
    {
        try
        {
            PerformanceAdviser adviser = PerformanceAdviser.GetPerformanceAdviser();
            PerformanceAdviserRuleId? ruleId = adviser.GetAllRuleIds()
                .FirstOrDefault(candidate => candidate == UnusedFamiliesAndTypesRuleId);
            if (ruleId is null)
                return new HashSet<ElementId>();

            IList<FailureMessage> messages = adviser.ExecuteRules(
                document, new List<PerformanceAdviserRuleId> { ruleId });
            return messages.Count == 0
                ? new HashSet<ElementId>()
                : new HashSet<ElementId>(messages[0].GetFailingElements());
        }
        catch (Autodesk.Revit.Exceptions.InternalException)
        {
            return new HashSet<ElementId>();
        }
        catch (Autodesk.Revit.Exceptions.InvalidOperationException)
        {
            return new HashSet<ElementId>();
        }
    }
#endif
}
