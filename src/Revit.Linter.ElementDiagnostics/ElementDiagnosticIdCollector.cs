using System.Reflection;

namespace Revit.Linter.ElementDiagnostics;

internal static class ElementDiagnosticIdCollector
{
    // Every built-in diagnostic carries the BuiltIn tag plus the document-kind tags that mirror its
    // document filter: a check that rejects family documents gets Project, one that only accepts them
    // gets Family, and a check that runs in both gets both tags.
    private static readonly string[] ProjectAndFamily = [DiagnosticTags.Project, DiagnosticTags.Family];

    public static readonly ElementDiagnosticId AnyConnectorsNotConnected = Create("SYST001", DiagnosticSeverity.Message, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId FamilyUnused = Create("SHRD001", DiagnosticSeverity.Message, ProjectAndFamily);
    public static readonly ElementDiagnosticId FamilySymbolUnused = Create("SHRD002", DiagnosticSeverity.Message, ProjectAndFamily);
    public static readonly ElementDiagnosticId FamilyInstanceMirrored = Create("SHRD003", DiagnosticSeverity.Warning, ProjectAndFamily);
    public static readonly ElementDiagnosticId ViewUnplaced = Create("SHRD004", DiagnosticSeverity.Warning, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId WallTopOffsetUnconnected = Create("SHRD005", DiagnosticSeverity.Warning, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId LocationLineElementWithTolerantCoordinates = Create("SHRD006", DiagnosticSeverity.Warning, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId LocationLineElementWithTolerantLength = Create("SHRD007", DiagnosticSeverity.Warning, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId LevelHeightIsTolerant = Create("SHRD008", DiagnosticSeverity.Warning, DiagnosticTags.Project);
#if AFTER2023
    public static readonly ElementDiagnosticId FloorWithTolerantSketchCoordinates = Create("SHRD009", DiagnosticSeverity.Warning, DiagnosticTags.Project);
#endif
    public static readonly ElementDiagnosticId FamilyInstanceElevationIsTolerant = Create("SHRD0010", DiagnosticSeverity.Warning, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId ParameterElementUnused = Create("SHRD0011", DiagnosticSeverity.Warning, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId FamilyInstanceLevelIsNearest = Create("SHRD0012", DiagnosticSeverity.Message, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId ModelCurveExists = Create("SHRD0013", DiagnosticSeverity.Message, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId DetailCurveExists = Create("SHRD0014", DiagnosticSeverity.Message, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId TextNoteExists = Create("SHRD0015", DiagnosticSeverity.Message, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId ImportInstanceExists = Create("SHRD0016", DiagnosticSeverity.Message, ProjectAndFamily);
    public static readonly ElementDiagnosticId RoomUnplaced = Create("ARCH001", DiagnosticSeverity.Error, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId RoomNotEnclosed = Create("ARCH002", DiagnosticSeverity.Error, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId RoomIsRedundant = Create("ARCH003", DiagnosticSeverity.Error, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId WallHeightIsTolerant = Create("ARCH004", DiagnosticSeverity.Warning, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId SheetEmpty = Create("SHRD0017", DiagnosticSeverity.Warning, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId ViewOnSheetWithoutTemplate = Create("SHRD0018", DiagnosticSeverity.Warning, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId SheetWithoutTitleBlock = Create("SHRD0019", DiagnosticSeverity.Error, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId SheetWithMultipleTitleBlocks = Create("SHRD0020", DiagnosticSeverity.Error, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId GroupNested = Create("SHRD0021", DiagnosticSeverity.Warning, ProjectAndFamily);
    public static readonly ElementDiagnosticId MaterialUnused = Create("SHRD0022", DiagnosticSeverity.Message, ProjectAndFamily);
    public static readonly ElementDiagnosticId ProfileFamilySymbolUnused = Create("SHRD0023", DiagnosticSeverity.Message, DiagnosticTags.Project);
    public static readonly ElementDiagnosticId ReferencePlaneUnused = Create("SHRD0024", DiagnosticSeverity.Message, DiagnosticTags.Family);
    public static readonly ElementDiagnosticId UnfilledFamilyParameters = Create("SHRD0025", DiagnosticSeverity.Message, DiagnosticTags.Family);
    public static readonly ElementDiagnosticId DuplicateFamilyTypes = Create("SHRD0026", DiagnosticSeverity.Warning, DiagnosticTags.Family);
    public static readonly ElementDiagnosticId GroupExists = Create("SHRD0027", DiagnosticSeverity.Message, ProjectAndFamily);
    public static readonly ElementDiagnosticId DuplicateFamilySymbols = Create("SHRD0028", DiagnosticSeverity.Warning, ProjectAndFamily);
    public static readonly ElementDiagnosticId ReferenceLineUnused = Create("SHRD0029", DiagnosticSeverity.Message, DiagnosticTags.Family);
    public static readonly ElementDiagnosticId PurgeableElementUnused = Create("RVT003", DiagnosticSeverity.Message, ProjectAndFamily);
    public static readonly ElementDiagnosticId AssociatedArray = Create("SHRD0031", DiagnosticSeverity.Warning, ProjectAndFamily);
    public static readonly ElementDiagnosticId LargeArray = Create("SHRD0032", DiagnosticSeverity.Warning, ProjectAndFamily);
    public static readonly ElementDiagnosticId ArrayWithFormula = Create("SHRD0033", DiagnosticSeverity.Warning, ProjectAndFamily);
    public static readonly ElementDiagnosticId LargeRadialArray = Create("SHRD0034", DiagnosticSeverity.Warning, ProjectAndFamily);

    private static readonly Lazy<IReadOnlyList<ElementDiagnosticId>> _allDiagnosticIds =
        new(typeof(ElementDiagnosticIdCollector)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(ElementDiagnosticId))
            .Select(field => (ElementDiagnosticId)field.GetValue(null)!).Where(i => i != null)
            .ToList);

    internal static IReadOnlyList<ElementDiagnosticId> GetAllDiagnosticIds() => _allDiagnosticIds.Value;

    private static ElementDiagnosticId Create(string code, DiagnosticSeverity severity, params string[] systemTags) => new(
        code,
        ElementDiagnosticLocalizations.GetString($"{code}_description"),
        ElementDiagnosticLocalizations.GetString($"{code}_message"),
        severity,
        true,
        false,
        string.Empty,
        systemTags: [DiagnosticTags.BuiltIn, .. systemTags]);
}
