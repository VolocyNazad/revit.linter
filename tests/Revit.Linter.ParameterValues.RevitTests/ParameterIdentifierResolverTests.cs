using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Revit.Linter.ParameterValues.Services;

namespace Revit.Linter.ParameterValues.RevitTests;

public sealed class ParameterIdentifierResolverTests : RevitApiTest
{
    [Test]
    [Arguments("ALL_MODEL_INSTANCE_COMMENTS")]
    [Arguments("WALL_BASE_OFFSET")]
    public async Task GetIdentifierType_recognizes_built_in_parameter_name(string identifier) =>
        await Assert.That(ParameterIdentifierResolver.GetIdentifierType(identifier))
            .IsEqualTo(typeof(BuiltInParameter));

    [Test]
    public async Task GetIdentifierType_recognizes_shared_parameter_guid() =>
        await Assert.That(
                ParameterIdentifierResolver.GetIdentifierType("8a5b1f0c-2c7d-4c71-9a52-3f0c1f1b5d6a"))
            .IsEqualTo(typeof(Guid));

    [Test]
    [Arguments("Марка")]
    [Arguments("Comments")]
    [Arguments("ADSK_Тип помещения")]
    public async Task GetIdentifierType_uses_display_name_for_other_text(string identifier) =>
        await Assert.That(ParameterIdentifierResolver.GetIdentifierType(identifier))
            .IsEqualTo(typeof(string));

    [Test]
    public async Task Resolve_returns_null_for_missing_element() =>
        await Assert.That(
                ParameterIdentifierResolver.Resolve(null, "ALL_MODEL_INSTANCE_COMMENTS"))
            .IsNull();
}