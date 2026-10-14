namespace Revit.Linter.CollisionDiagnostics.Infrastructure;

internal static class CollisionQueryNames
{
    public const string BoundingBox = "collision-diagnostics:element-bounding-box";
    public const string Solids = "collision-diagnostics:element-solids";
    public const string ConnectedElementIds = "collision-diagnostics:connected-element-ids";
    public const string TargetElements = "collision-diagnostics:target-elements";
    public const string Groups = "collision-diagnostics:groups";
    public const string SpatialIndex = "collision-diagnostics:spatial-index";
    public const string PairIntersects = "collision-diagnostics:pair-intersects";
    public const string LinkGroups = "collision-diagnostics:link-groups";
    public const string LinkGroupsByInstance = "collision-diagnostics:link-groups-by-instance";
    public const string LinkSpatialIndex = "collision-diagnostics:link-spatial-index";
}