using Revit.Linter.ElementDependencyDefiners;
using StringToExpression.GrammerDefinitions;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Revit.Linter.Languages.Languages;

public static class ElementDependencyDefinerOperandDefinitions
{
    private static readonly (string Syntax, Type Type)[] Definitions =
    [
        ("insertedintohosttypeinstance", typeof(InstanceInsertsDependencyDefiner)),
        ("insulationhosttypeinstance", typeof(InstanceInsulationsDependencyDefiner)),
        ("membergrouptypeinstance", typeof(InstanceMembersDependencyDefiner)),
        ("subparenttypeinstance", typeof(InstanceSubComponentsDependencyDefiner)),
        ("placedinscopebox", typeof(InstancesInsideScopeBoxDependencyDefiner)),
        ("internalinsulation", typeof(InternalInsulationDependencyDefiner)),
        ("externalinsulation", typeof(ExternalInsulationDependencyDefiner)),
        ("insertedintohost", typeof(InsertsDependencyDefiner)),
        ("placedinroom", typeof(PlacedInsideRoomDependencyDefiner)),
        ("placedinspace", typeof(PlacedInsideSpaceDependencyDefiner)),
        ("insulationhosttype", typeof(MEPCurveHostTypeDependencyDefiner)),
        ("scopeboxowner", typeof(ScopeBoxDependencyDefiner)),
        ("insulationhost", typeof(MEPCurveHostDependencyDefiner)),
        ("membergroup", typeof(MembersDependencyDefiner)),
        ("parenttype", typeof(GeneralSuperComponentTypeDependencyDefiner)),
        ("roomowner", typeof(RoomDependencyDefiner)),
        ("spaceowner", typeof(SpaceDependencyDefiner)),
        ("grouptype", typeof(GeneralGroupTypeDependencyDefiner)),
        ("insulation", typeof(InsulationsDependencyDefiner)),
        ("connected", typeof(ConnectedDependencyDefiner)),
        ("hosttype", typeof(HostTypeDependencyDefiner)),
        ("instance", typeof(InstancesDependencyDefiner)),
        ("parent", typeof(GeneralSuperComponentDependencyDefiner)),
        ("group", typeof(GeneralGroupDependencyDefiner)),
        ("family", typeof(FamilyDependencyDefiner)),
        ("host", typeof(HostDependencyDefiner)),
        ("type", typeof(TypeDependencyDefiner)),
        ("sub", typeof(SubComponentsDependencyDefiner)),
        ("empty", typeof(EmptyDependencyDefiner)),
        ("me", typeof(InternalDependencyDefiner)),
    ];

    public static OperandDefinition[] Get()
        => [.. Definitions.Select(definition => new OperandDefinition(
            name: $"ELEMENT_DEFINER_{definition.Syntax.ToUpperInvariant()}",
            regex: $@"{Regex.Escape(definition.Syntax)}\b",
            expressionBuilder: _ => Expression.New(definition.Type)))];
}
